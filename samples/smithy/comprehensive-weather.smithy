$version: "2"

namespace smithy.example.weather

/// A comprehensive weather service demonstrating Smithy 2.0 features
@protocols([{name: "smithy.protocols#restJson1"}])
service WeatherService {
    version: "2024-01-01"
    operations: [GetWeather, CreateForecast]
    resources: [Forecast]
    errors: [WeatherServiceError]
}

/// Get current weather for a location
@readonly
@http(method: "GET", uri: "/weather/{locationId}")
operation GetWeather {
    input := {
        @httpLabel
        @required
        locationId: LocationId
        
        @httpQuery("units")
        units: TemperatureUnit = "celsius"
        
        @httpHeader("x-api-key")
        apiKey: String
    }
    output := {
        @required
        temperature: Temperature
        
        @required
        conditions: WeatherConditions
        
        humidity: Integer
        
        @httpResponseCode
        statusCode: Integer
    }
    errors: [LocationNotFound, InvalidApiKey]
}

/// Create a weather forecast
@idempotent
@http(method: "PUT", uri: "/forecasts/{forecastId}")
operation CreateForecast {
    input := {
        @httpLabel
        @required
        forecastId: ForecastId
        
        @httpPayload
        @required
        forecast: ForecastData
    }
    output := {
        @required
        forecastId: ForecastId
        
        @required
        createdAt: Timestamp
    }
    errors: [InvalidForecastData]
}

/// Weather forecast resource
resource Forecast {
    identifiers: { forecastId: ForecastId }
    properties: { 
        temperature: Temperature
        conditions: WeatherConditions
        validUntil: Timestamp
    }
    read: GetForecast
    create: CreateForecast
    delete: DeleteForecast
}

@readonly
@http(method: "GET", uri: "/forecasts/{forecastId}")
operation GetForecast {
    input := {
        @httpLabel
        @required
        forecastId: ForecastId
    }
    output := for Forecast {
        $forecastId
        $temperature
        $conditions
        $validUntil
    }
}

@idempotent
@http(method: "DELETE", uri: "/forecasts/{forecastId}")
operation DeleteForecast {
    input := {
        @httpLabel
        @required
        forecastId: ForecastId
    }
}

// Simple types
@pattern("^[A-Z]{2}-[0-9]{4}$")
string LocationId

@pattern("^forecast-[0-9]+$")
string ForecastId

@range(min: -100, max: 100)
integer Temperature

// Enum with explicit values
enum TemperatureUnit {
    @enumValue("C")
    CELSIUS = "celsius"
    
    @enumValue("F") 
    FAHRENHEIT = "fahrenheit"
    
    @enumValue("K")
    KELVIN = "kelvin"
}

// String enum
enum WeatherConditions {
    SUNNY
    CLOUDY
    RAINY
    SNOWY
    STORMY
}

// IntEnum
intEnum AlertLevel {
    LOW = 1
    MEDIUM = 2
    HIGH = 3
    CRITICAL = 4
}

// Structure with various members
structure ForecastData {
    @required
    temperature: Temperature
    
    @required
    conditions: WeatherConditions
    
    @default(0)
    humidity: Integer
    
    @sensitive
    internalNotes: String
    
    @deprecated(message: "Use alerts instead")
    severity: AlertLevel
    
    alerts: AlertList
}

// List with constraints
@length(min: 0, max: 10)
list AlertList {
    member: Alert
}

// Map example
map WeatherData {
    key: String
    value: Temperature
}

// Union type
union Alert {
    temperature: TemperatureAlert
    wind: WindAlert
    precipitation: PrecipitationAlert
}

structure TemperatureAlert {
    @required
    threshold: Temperature
    
    @required
    type: AlertType
}

structure WindAlert {
    @required
    @range(min: 0, max: 200)
    speedKmh: Integer
    
    direction: WindDirection
}

structure PrecipitationAlert {
    @required
    @range(min: 0, max: 100) 
    probabilityPercent: Integer
    
    type: PrecipitationType
}

enum AlertType {
    HEAT_WARNING
    FREEZE_WARNING
    EXTREME_TEMPERATURE
}

enum WindDirection {
    NORTH
    SOUTH  
    EAST
    WEST
    NORTHEAST
    NORTHWEST
    SOUTHEAST
    SOUTHWEST
}

enum PrecipitationType {
    RAIN
    SNOW
    SLEET
    HAIL
}

// Error structures
@error("client")
structure LocationNotFound {
    @required
    message: String
    
    @required
    locationId: LocationId
}

@error("client") 
structure InvalidApiKey {
    @required
    message: String
}

@error("client")
structure InvalidForecastData {
    @required
    message: String
    
    validationErrors: ValidationErrorList
}

@error("server")
structure WeatherServiceError {
    @required
    message: String
    
    @required
    code: String
    
    retryAfterSeconds: Integer
}

list ValidationErrorList {
    member: ValidationError
}

structure ValidationError {
    @required
    field: String
    
    @required
    message: String
    
    @required
    code: String
}
