using IndustrialBackendDemo.Application.Abstractions.Persistence;
using IndustrialBackendDemo.Application.Common.Results;
using IndustrialBackendDemo.Application.Devices.GetDevice;
using IndustrialBackendDemo.Domain.Devices;

namespace IndustrialBackendDemo.UnitTests.Devices.GetDevice;

public sealed class GetDeviceHandlerTests
{
    private static readonly DeviceId TestDeviceId = DeviceId.New();

    private const string DeviceName = "Industrial Sensor";
    private const string SerialNumber = "SN-2026-0001";

    private static GetDeviceQuery CreateQuery()
    {
        return new GetDeviceQuery(TestDeviceId);
    }

    private static Device CreateDevice()
    {
        return new Device(TestDeviceId, DeviceName, SerialNumber);
    }

    private static GetDeviceHandler CreateHandler(FakeDeviceRepository repository)
    {
        return new GetDeviceHandler(repository);
    }

    [Fact]
    public async Task HandleAsyncShouldReturnDeviceSuccessfully()
    {
        Device expectedDevice = CreateDevice();

        FakeDeviceRepository repository = new()
        {
            ExistingDevice = expectedDevice
        };

        GetDeviceHandler handler = CreateHandler(repository);

        Result<Device> result = await handler.HandleAsync(CreateQuery());

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);

        Assert.Same(expectedDevice, result.Value);

        Assert.Equal(DeviceName, result.Value.Name);
        Assert.Equal(SerialNumber, result.Value.SerialNumber);
        Assert.Equal(TestDeviceId, result.Value.Id);

        Assert.Equal(1, repository.GetByIdCallCount);
    }

    [Fact]
    public async Task HandleAsyncShouldReturnNotFoundErrorWhenDeviceDoesNotExist()
    {
        FakeDeviceRepository repository = new();

        GetDeviceHandler handler = CreateHandler(repository);

        Result<Device> result = await handler.HandleAsync(CreateQuery());

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);

        Assert.Equal("Device.NotFound", result.Error.Code);

        Assert.Equal("The specified device was not found.", result.Error.Description);
        Assert.Equal(1, repository.GetByIdCallCount);
    }

    [Fact]
    public async Task HandleAsyncShouldPassDeviceIdToRepository()
    {
        FakeDeviceRepository repository = new()
        {
            ExistingDevice = CreateDevice()
        };

        GetDeviceHandler handler = CreateHandler(repository);

        await handler.HandleAsync(CreateQuery());

        Assert.Equal(TestDeviceId, repository.RequestedDeviceId);
    }

    [Fact]
    public async Task HandleAsyncShouldRejectNullQuery()
    {
        FakeDeviceRepository repository = new();

        GetDeviceHandler handler = CreateHandler(repository);

        await Assert.ThrowsAsync<ArgumentNullException>(() => handler.HandleAsync(null!));

        Assert.Equal(0, repository.GetByIdCallCount);
    }

    [Fact]
    public async Task HandleAsyncShouldPropagateRepositoryException()
    {
        InvalidOperationException expectedException = new("Repository query failed.");

        FakeDeviceRepository repository = new()
        {
            QueryException = expectedException
        };

        GetDeviceHandler handler = CreateHandler(repository);

        InvalidOperationException actualException =
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(CreateQuery()));

        Assert.Same(expectedException, actualException);
        Assert.Equal(1, repository.GetByIdCallCount);
    }

    [Fact]
    public async Task HandleAsyncShouldPassCancellationTokenToRepository()
    {
        using CancellationTokenSource cancellationTokenSource = new();

        FakeDeviceRepository repository = new()
        {
            ExistingDevice = CreateDevice()
        };

        GetDeviceHandler handler = CreateHandler(repository);

        await handler.HandleAsync(CreateQuery(), cancellationTokenSource.Token);

        Assert.Equal(cancellationTokenSource.Token, repository.QueryCancellationToken);
    }

    [Fact]
    public async Task HandleAsyncShouldPropagateCancellation()
    {
        using CancellationTokenSource cancellationTokenSource = new();

        await cancellationTokenSource.CancelAsync();

        FakeDeviceRepository repository = new();

        GetDeviceHandler handler = CreateHandler(repository);

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => handler.HandleAsync(CreateQuery(), cancellationTokenSource.Token)
        );

        Assert.Equal(1, repository.GetByIdCallCount);
        Assert.Equal(cancellationTokenSource.Token, repository.QueryCancellationToken);
    }

    private sealed class FakeDeviceRepository : IDeviceRepository
    {
        public Device? ExistingDevice { get; init; }
        public Exception? QueryException { get; init; }
        public int GetByIdCallCount { get; private set; }
        public DeviceId RequestedDeviceId { get; private set; }
        public CancellationToken QueryCancellationToken { get; private set; }

        public Task AddAsync(Device device, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<Device?> GetByIdAsync(DeviceId id, CancellationToken cancellationToken = default)
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

        public Task<Device?> GetBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Device?>(null);
        }

        public Task UpdateAsync(Device device, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
