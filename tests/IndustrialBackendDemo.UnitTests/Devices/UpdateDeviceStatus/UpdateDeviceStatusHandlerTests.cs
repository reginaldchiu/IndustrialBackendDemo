
using IndustrialBackendDemo.Application.Abstractions.Persistence;
using IndustrialBackendDemo.Application.Common.Results;
using IndustrialBackendDemo.Application.Devices.UpdateDeviceStatus;
using IndustrialBackendDemo.Domain.Devices;

namespace IndustrialBackendDemo.UnitTests.Devices.UpdateDeviceStatus;

public sealed class UpdateDeviceStatusHandlerTests
{
    private static readonly DeviceId TestDeviceId = DeviceId.New();

    private const string DeviceName = "Industrial Sensor";
    private const string SerialNumber = "SN-2026-0001";

    private static UpdateDeviceStatusCommand CreateCommand(
        DeviceStatus status = DeviceStatus.Online
    )
    {
        return new UpdateDeviceStatusCommand(TestDeviceId, status);
    }

    private static Device CreateDevice()
    {
        return new Device(
            TestDeviceId,
            DeviceName,
            SerialNumber
        );
    }

    private static Device CreateDecommissionedDevice()
    {
        Device device = CreateDevice();
        device.ChangeStatus(DeviceStatus.Decommissioned);

        return device;
    }

    private static UpdateDeviceStatusHandler CreateHandler(
        FakeDeviceRepository repository
    )
    {
        return new UpdateDeviceStatusHandler(repository);
    }

    [Fact]
    public async Task HandleAsyncShouldUpdateDeviceStatusSuccessfully()
    {
        Device device = CreateDevice();

        FakeDeviceRepository repository = new()
        {
            ExistingDevice = device,
        };

        UpdateDeviceStatusHandler handler = CreateHandler(repository);

        Result<Device> result = await handler.HandleAsync(CreateCommand());

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);

        Assert.Same(device, result.Value);
        Assert.Equal(DeviceStatus.Online, result.Value.Status);

        Assert.Equal(1, repository.GetByIdCallCount);
        Assert.Equal(1, repository.UpdateCallCount);

        Assert.Same(device, repository.UpdatedDevice);
    }

    [Fact]
    public async Task HandleAsyncShouldReturnNotFoundWhenDeviceDoesNotExist()
    {
        FakeDeviceRepository repository = new();

        UpdateDeviceStatusHandler handler = CreateHandler(repository);

        Result<Device> result = await handler.HandleAsync(CreateCommand());

        Assert.True(result.IsFailure);
        Assert.Equal("Device.NotFound", result.Error.Code);

        Assert.Equal(1, repository.GetByIdCallCount);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    [Fact]
    public async Task HandleAsyncShouldReturnInvalidTransitionWhenTransitionIsNotAllowed()
    {
        Device device = CreateDecommissionedDevice();

        FakeDeviceRepository repository = new()
        {
            ExistingDevice = device,
        };

        UpdateDeviceStatusHandler handler = CreateHandler(repository);

        Result<Device> result = await handler.HandleAsync(
            CreateCommand(DeviceStatus.Online)
        );

        Assert.True(result.IsFailure);

        Assert.Equal(
            "Device.InvalidStatusTransition",
            result.Error.Code
        );

        Assert.Equal(DeviceStatus.Decommissioned, device.Status);

        Assert.Equal(1, repository.GetByIdCallCount);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    [Fact]
    public async Task HandleAsyncShouldReturnInvalidTransitionWhenStatusIsUnchanged()
    {
        Device device = CreateDevice();

        FakeDeviceRepository repository = new()
        {
            ExistingDevice = device,
        };

        UpdateDeviceStatusHandler handler = CreateHandler(repository);

        Result<Device> result = await handler.HandleAsync(
            CreateCommand(DeviceStatus.Offline)
        );

        Assert.True(result.IsFailure);

        Assert.Equal(
            "Device.InvalidStatusTransition",
            result.Error.Code
        );

        Assert.Equal(DeviceStatus.Offline, device.Status);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    [Fact]
    public async Task HandleAsyncShouldReturnInvalidStatusForUndefinedStatus()
    {
        Device device = CreateDevice();

        FakeDeviceRepository repository = new()
        {
            ExistingDevice = device,
        };

        UpdateDeviceStatusHandler handler = CreateHandler(repository);

        Result<Device> result = await handler.HandleAsync(
            CreateCommand((DeviceStatus)999)
        );

        Assert.True(result.IsFailure);

        Assert.Equal("Device.InvalidStatus", result.Error.Code);

        Assert.Equal(DeviceStatus.Offline, device.Status);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    [Fact]
    public async Task HandleAsyncShouldPassDeviceIdToRepository()
    {
        FakeDeviceRepository repository = new()
        {
            ExistingDevice = CreateDevice(),
        };

        UpdateDeviceStatusHandler handler = CreateHandler(repository);

        await handler.HandleAsync(CreateCommand());

        Assert.Equal(TestDeviceId, repository.RequestedDeviceId);
    }

    [Fact]
    public async Task HandleAsyncShouldRejectNullCommand()
    {
        FakeDeviceRepository repository = new();

        UpdateDeviceStatusHandler handler = CreateHandler(repository);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!)
        );

        Assert.Equal(0, repository.GetByIdCallCount);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    [Fact]
    public async Task HandleAsyncShouldPropagateRepositoryQueryException()
    {
        InvalidOperationException expectedException = new(
            "Repository query failed."
        );

        FakeDeviceRepository repository = new()
        {
            QueryException = expectedException,
        };

        UpdateDeviceStatusHandler handler = CreateHandler(repository);

        InvalidOperationException actualException =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.HandleAsync(CreateCommand())
            );

        Assert.Same(expectedException, actualException);

        Assert.Equal(1, repository.GetByIdCallCount);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    [Fact]
    public async Task HandleAsyncShouldPropagateRepositoryUpdateException()
    {
        Device device = CreateDevice();

        InvalidOperationException expectedException = new(
            "Repository update failed."
        );

        FakeDeviceRepository repository = new()
        {
            ExistingDevice = device,
            UpdateException = expectedException,
        };

        UpdateDeviceStatusHandler handler = CreateHandler(repository);

        InvalidOperationException actualException =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.HandleAsync(CreateCommand())
            );

        Assert.Same(expectedException, actualException);

        Assert.Equal(DeviceStatus.Online, device.Status);
        Assert.Equal(1, repository.GetByIdCallCount);
        Assert.Equal(1, repository.UpdateCallCount);
    }

    [Fact]
    public async Task HandleAsyncShouldPassCancellationTokenToRepository()
    {
        using CancellationTokenSource cancellationTokenSource = new();

        FakeDeviceRepository repository = new()
        {
            ExistingDevice = CreateDevice(),
        };

        UpdateDeviceStatusHandler handler = CreateHandler(repository);

        await handler.HandleAsync(
            CreateCommand(),
            cancellationTokenSource.Token
        );

        Assert.Equal(
            cancellationTokenSource.Token,
            repository.QueryCancellationToken
        );

        Assert.Equal(
            cancellationTokenSource.Token,
            repository.UpdateCancellationToken
        );
    }

    [Fact]
    public async Task HandleAsyncShouldPropagateCancellation()
    {
        using CancellationTokenSource cancellationTokenSource = new();

        await cancellationTokenSource.CancelAsync();

        FakeDeviceRepository repository = new()
        {
            ExistingDevice = CreateDevice(),
        };

        UpdateDeviceStatusHandler handler = CreateHandler(repository);

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => handler.HandleAsync(
                CreateCommand(),
                cancellationTokenSource.Token
            )
        );

        Assert.Equal(1, repository.GetByIdCallCount);
        Assert.Equal(0, repository.UpdateCallCount);

        Assert.Equal(
            cancellationTokenSource.Token,
            repository.QueryCancellationToken
        );
    }

    private sealed class FakeDeviceRepository : IDeviceRepository
    {
        public Device? ExistingDevice { get; init; }

        public Exception? QueryException { get; init; }

        public Exception? UpdateException { get; init; }

        public int GetByIdCallCount { get; private set; }

        public int UpdateCallCount { get; private set; }

        public DeviceId RequestedDeviceId { get; private set; }

        public Device? UpdatedDevice { get; private set; }

        public CancellationToken QueryCancellationToken { get; private set; }

        public CancellationToken UpdateCancellationToken { get; private set; }

        public Task<Device?> GetByIdAsync(
            DeviceId id,
            CancellationToken cancellationToken = default
        )
        {
            GetByIdCallCount++;

            RequestedDeviceId = id;
            QueryCancellationToken = cancellationToken;

            cancellationToken.ThrowIfCancellationRequested();

            if (QueryException is not null)
            {
                return Task.FromException<Device?>(QueryException);
            }

            return Task.FromResult(ExistingDevice);
        }

        public Task UpdateAsync(
            Device device,
            CancellationToken cancellationToken = default
        )
        {
            UpdateCallCount++;

            UpdatedDevice = device;
            UpdateCancellationToken = cancellationToken;

            cancellationToken.ThrowIfCancellationRequested();

            if (UpdateException is not null)
            {
                return Task.FromException(UpdateException);
            }

            return Task.CompletedTask;
        }

        public Task AddAsync(
            Device device,
            CancellationToken cancellationToken = default
        )
        {
            return Task.CompletedTask;
        }

        public Task<Device?> GetBySerialNumberAsync(
            string serialNumber,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult<Device?>(null);
        }
    }
}
