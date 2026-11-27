using IndustrialBackendDemo.Api.Contracts.Devices;
using IndustrialBackendDemo.Application.Common.Results;
using IndustrialBackendDemo.Application.Devices.GetDevice;
using IndustrialBackendDemo.Application.Devices.RegisterDevice;
using IndustrialBackendDemo.Application.Devices.UpdateDeviceStatus;
using IndustrialBackendDemo.Domain.Devices;

namespace IndustrialBackendDemo.Api.Endpoints.Devices;

public static class DeviceEndpoints
{
    private const string _deviceRouteName = "GetDeviceById";

    public static IEndpointRouteBuilder MapDeviceEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        RouteGroupBuilder group = endpoints
            .MapGroup("/api/devices")
            .WithTags("Devices");

        group.MapPost("/", RegisterDevice)
            .WithName("RegisterDevice")
            .WithSummary("Register a device")
            .Produces<DeviceResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/{id}", GetDevice)
            .WithName(_deviceRouteName)
            .WithSummary("Get a device by ID")
            .Produces<DeviceResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPatch("/{id}/status", UpdateDeviceStatus)
            .WithName("UpdateDeviceStatus")
            .WithSummary("Update a device status")
            .Produces<DeviceResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<IResult> RegisterDevice(
        RegisterDeviceRequest? request,
        RegisterDeviceHandler handler,
        HttpContext httpContext
    )
    {
        Dictionary<string, string[]> errors = [];

        if (request is null)
        {
            errors.Add("request", ["A request body is required."]);
        }
        else
        {
            ValidateRequiredString(
                errors,
                nameof(request.Name),
                request.Name,
                "Name"
            );

            ValidateRequiredString(
                errors,
                nameof(request.SerialNumber),
                request.SerialNumber,
                "Serial number"
            );
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        RegisterDeviceCommand command = new(
            request!.Name!,
            request.SerialNumber!
        );

        Result<Device> result = await handler.HandleAsync(
            command,
            httpContext.RequestAborted
        );

        if (result.IsFailure)
        {
            return MapApplicationError(result.Error);
        }

        var response = DeviceResponse.FromDomain(result.Value);

        var location = $"/api/devices/{response.Id:D}";

        return Results.Created(location, response);
    }

    private static async Task<IResult> GetDevice(
        string id,
        GetDeviceHandler handler,
        HttpContext httpContext
    )
    {
        if (!Guid.TryParse(id, out Guid guid) || guid == Guid.Empty)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["id"] = ["A valid, non-empty device ID is required."]
                }
            );
        }

        GetDeviceQuery query = new(new DeviceId(guid));

        Result<Device> result = await handler.HandleAsync(
            query,
            httpContext.RequestAborted
        );

        if (result.IsFailure)
        {
            return MapApplicationError(result.Error);
        }

        return Results.Ok(DeviceResponse.FromDomain(result.Value));
    }

    private static async Task<IResult> UpdateDeviceStatus(
        string id,
        UpdateDeviceStatusRequest? request,
        UpdateDeviceStatusHandler handler,
        HttpContext httpContext
    )
    {
        if (!Guid.TryParse(id, out Guid guid) || guid == Guid.Empty)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["id"] = ["A valid, non-empty device ID is required."]
                }
            );
        }

        if (request is null || string.IsNullOrWhiteSpace(request.Status))
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["status"] = ["A device status is required."]
                }
            );
        }

        if (!TryParseDeviceStatus(request.Status, out DeviceStatus status))
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["status"] =
                    [
                        "Status must be one of: Offline, Online, Maintenance, Decommissioned."
                    ]
                }
            );
        }

        UpdateDeviceStatusCommand command = new(
            new DeviceId(guid),
            status
        );

        Result<Device> result = await handler.HandleAsync(
            command,
            httpContext.RequestAborted
        );

        if (result.IsFailure)
        {
            return MapApplicationError(result.Error);
        }

        return Results.Ok(DeviceResponse.FromDomain(result.Value));
    }

    private static void ValidateRequiredString(
        Dictionary<string, string[]> errors,
        string key,
        string? value,
        string displayName
    )
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[key] = [$"{displayName} is required."];
            return;
        }

        if (value.Length > 100)
        {
            errors[key] = [$"{displayName} cannot exceed 100 characters."];
        }
    }

    private static bool TryParseDeviceStatus(
        string value,
        out DeviceStatus status
    )
    {
        status = default;

        // Only accept actual enum names.
        // This prevents numeric values such as "1" from being accepted.
        var isKnownName = Enum.GetNames<DeviceStatus>()
            .Any(name => string.Equals(
                name,
                value,
                StringComparison.OrdinalIgnoreCase
            ));

        if (!isKnownName)
        {
            return false;
        }

        return Enum.TryParse(
            value,
            ignoreCase: true,
            out status
        );
    }

    private static IResult MapApplicationError(ApplicationError error)
    {
        return error.Code switch
        {
            "Device.NotFound" => Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Device not found",
                detail: error.Description,
                extensions: CreateErrorExtensions(error)
            ),

            "Device.DuplicateSerialNumber" => Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Duplicate serial number",
                detail: error.Description,
                extensions: CreateErrorExtensions(error)
            ),

            "Device.InvalidStatus" => Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid device status",
                detail: error.Description,
                extensions: CreateErrorExtensions(error)
            ),

            "Device.InvalidStatusTransition" => Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Invalid device status transition",
                detail: error.Description,
                extensions: CreateErrorExtensions(error)
            ),

            _ => throw new InvalidOperationException(
                $"Unsupported application error: {error.Code}"
            )
        };
    }

    private static Dictionary<string, object?> CreateErrorExtensions(
        ApplicationError error
    )
    {
        return new Dictionary<string, object?>
        {
            ["code"] = error.Code
        };
    }
}
