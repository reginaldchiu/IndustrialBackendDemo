using IndustrialBackendDemo.Application.Abstractions.Persistence;
using IndustrialBackendDemo.Application.Common.Results;
using IndustrialBackendDemo.Application.Devices.RegisterDevice;
using IndustrialBackendDemo.Domain.Devices;

namespace IndustrialBackendDemo.UnitTests.Devices.RegisterDevice;

public sealed class RegisterDeviceHandlerTests
{
    private const string DeviceName = "Industrial Sensor";
    private const string SerialNumber = "SN-2026-0001";

    private static RegisterDeviceCommand CreateCommand()
    {
        return new RegisterDeviceCommand(DeviceName, SerialNumber);
    }

    private static Device CreateExistingDevice()
    {
        return new Device(DeviceId.New(), "Existing Sensor", SerialNumber);
    }

    private static RegisterDeviceHandler CreateHandler(FakeDeviceRepository repository)
    {
        return new RegisterDeviceHandler(repository);
    }

    [Fact]
    public async Task HandleAsyncShouldRegisterDeviceSuccessfully()
    {
        FakeDeviceRepository repository = new();
        RegisterDeviceHandler handler = CreateHandler(repository);

        Result<Device> result = await handler.HandleAsync(CreateCommand());

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);

        Assert.Equal(DeviceName, result.Value.Name);
        Assert.Equal(SerialNumber, result.Value.SerialNumber);

        Assert.Equal(DeviceStatus.Offline, result.Value.Status);
        Assert.NotEqual(Guid.Empty, result.Value.Id.Value);

        Assert.NotNull(repository.AddedDevice);
        Assert.Same(result.Value, repository.AddedDevice);

        Assert.Equal(1, repository.GetBySerialNumberCallCount);
        Assert.Equal(1, repository.AddCallCount);
    }

    [Fact]
    public async Task HandleAsyncShouldReturnDuplicateSerialNumberError()
    {
        FakeDeviceRepository repository = new() { ExistingDevice = CreateExistingDevice() };

        RegisterDeviceHandler handler = CreateHandler(repository);

        Result<Device> result = await handler.HandleAsync(CreateCommand());

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);

        Assert.Equal("Device.DuplicateSerialNumber", result.Error.Code);
        Assert.Equal(
            "A device with the specified serial number already exists.",
            result.Error.Description
        );

        Assert.Equal(1, repository.GetBySerialNumberCallCount);
        Assert.Equal(0, repository.AddCallCount);
        Assert.Null(repository.AddedDevice);
    }

    [Fact]
    public async Task HandleAsyncShouldPassSerialNumberToRepository()
    {
        FakeDeviceRepository repository = new();
        RegisterDeviceHandler handler = CreateHandler(repository);

        await handler.HandleAsync(CreateCommand());

        Assert.Equal(SerialNumber, repository.RequestedSerialNumber);
    }

    [Fact]
    public async Task HandleAsyncShouldPropagateQueryException()
    {
        InvalidOperationException expectedException = new("Repository query failed.");

        FakeDeviceRepository repository = new() { QueryException = expectedException };

        RegisterDeviceHandler handler = CreateHandler(repository);

        InvalidOperationException actualException =
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.HandleAsync(CreateCommand())
            );

        Assert.Same(expectedException, actualException);
        Assert.Equal(0, repository.AddCallCount);
    }

    [Fact]
    public async Task HandleAsyncShouldPropagateAddException()
    {
        InvalidOperationException expectedException = new("Repository add failed.");

        FakeDeviceRepository repository = new() { AddException = expectedException };

        RegisterDeviceHandler handler = CreateHandler(repository);

        InvalidOperationException actualException =
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.HandleAsync(CreateCommand())
            );

        Assert.Same(expectedException, actualException);
        Assert.Equal(1, repository.AddCallCount);
    }

    [Fact]
    public async Task HandleAsyncShouldPropagateCancellationDuringQuery()
    {
        using CancellationTokenSource cancellationTokenSource = new();
        await cancellationTokenSource.CancelAsync();

        FakeDeviceRepository repository = new();
        RegisterDeviceHandler handler = CreateHandler(repository);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            handler.HandleAsync(CreateCommand(), cancellationTokenSource.Token)
        );

        Assert.Equal(1, repository.GetBySerialNumberCallCount);
        Assert.Equal(0, repository.AddCallCount);

        Assert.Equal(cancellationTokenSource.Token, repository.QueryCancellationToken);
    }

    [Fact]
    public async Task HandleAsyncShouldPropagateCancellationDuringAdd()
    {
        using CancellationTokenSource cancellationTokenSource = new();

        FakeDeviceRepository repository = new()
        {
            OnQuery = () => cancellationTokenSource.Cancel(),
        };

        RegisterDeviceHandler handler = CreateHandler(repository);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            handler.HandleAsync(CreateCommand(), cancellationTokenSource.Token)
        );

        Assert.Equal(1, repository.GetBySerialNumberCallCount);
        Assert.Equal(1, repository.AddCallCount);

        Assert.Equal(cancellationTokenSource.Token, repository.AddCancellationToken);
    }

    [Fact]
    public async Task HandleAsyncShouldPassCancelationTokenToRepository()
    {
        using CancellationTokenSource cancellationTokenSource = new();

        FakeDeviceRepository repository = new();
        RegisterDeviceHandler handler = CreateHandler(repository);

        await handler.HandleAsync(CreateCommand(), cancellationTokenSource.Token);

        Assert.Equal(cancellationTokenSource.Token, repository.QueryCancellationToken);
        Assert.Equal(cancellationTokenSource.Token, repository.AddCancellationToken);
    }

    [Fact]
    public async Task HandleAsyncShouldCreateDeviceWithUtcTimestamps()
    {
        FakeDeviceRepository repository = new();
        RegisterDeviceHandler handler = CreateHandler(repository);

        Result<Device> result = await handler.HandleAsync(CreateCommand());

        Assert.True(result.IsSuccess);
        Assert.Equal(TimeSpan.Zero, result.Value.CreatedAtUtc.Offset);
        Assert.Equal(TimeSpan.Zero, result.Value.UpdatedAtUtc.Offset);

        Assert.Equal(result.Value.CreatedAtUtc, result.Value.UpdatedAtUtc);
    }

    [Fact]
    public async Task HandleAsyncShouldCreateDeviceWithNewId()
    {
        FakeDeviceRepository repository = new();
        RegisterDeviceHandler handler = CreateHandler(repository);

        Result<Device> result = await handler.HandleAsync(CreateCommand());

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value.Id.Value);
    }

    private sealed class FakeDeviceRepository : IDeviceRepository
    {
        public Device? ExistingDevice { get; init; }
        public Device? AddedDevice { get; private set; }
        public Exception? QueryException { get; init; }
        public Exception? AddException { get; init; }
        public Action? OnQuery { get; init; }
        public int GetBySerialNumberCallCount { get; private set; }
        public int AddCallCount { get; private set; }
        public string? RequestedSerialNumber { get; private set; }
        public CancellationToken QueryCancellationToken { get; private set; }
        public CancellationToken AddCancellationToken { get; private set; }

        public Task AddAsync(Device device, CancellationToken cancellationToken = default)
        {
            AddCallCount++;
            AddCancellationToken = cancellationToken;

            cancellationToken.ThrowIfCancellationRequested();

            if (AddException is not null)
                return Task.FromException(AddException);

            AddedDevice = device;

            return Task.CompletedTask;
        }

        public Task<Device?> GetByIdAsync(
            DeviceId id,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult<Device?>(null);
        }

        public Task<Device?> GetBySerialNumberAsync(
            string serialNumber,
            CancellationToken cancellationToken = default
        )
        {
            GetBySerialNumberCallCount++;
            RequestedSerialNumber = serialNumber;
            QueryCancellationToken = cancellationToken;

            cancellationToken.ThrowIfCancellationRequested();

            if (QueryException is not null)
                return Task.FromException<Device?>(QueryException);

            OnQuery?.Invoke();

            return Task.FromResult(ExistingDevice);
        }

        public Task UpdateAsync(Device device, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
