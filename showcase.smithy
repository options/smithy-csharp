$version: "2"

namespace example.showcase

/// A showcase service demonstrating working Smithy 2.0 features
@protocols([{name: "smithy.protocols#restJson1"}])
service ShowcaseService {
    version: "2024-01-01"
    operations: [GetItem, CreateItem]
    errors: [ValidationError, NotFoundError]
}

/// Get an item by ID
@readonly
@http(method: "GET", uri: "/items/{itemId}")
operation GetItem {
    input: GetItemRequest
    output: GetItemResponse
    errors: [NotFoundError]
}

/// Create a new item
@idempotent
@http(method: "PUT", uri: "/items/{itemId}")
operation CreateItem {
    input: CreateItemRequest
    output: CreateItemResponse
    errors: [ValidationError]
}

structure GetItemRequest {
    @httpLabel
    @required
    itemId: ItemId
}

structure GetItemResponse {
    @required
    item: Item
}

structure CreateItemRequest {
    @httpLabel
    @required
    itemId: ItemId
    
    @httpPayload
    @required
    item: Item
}

structure CreateItemResponse {
    @required
    itemId: ItemId
    
    @required
    createdAt: Timestamp
}

structure Item {
    @required
    name: String
    
    @required
    category: Category
    
    @default(0)
    price: Integer
    
    @sensitive
    internalNotes: String
    
    @deprecated(message: "Use tags instead")
    oldField: String
}

// Simple type with constraint
@pattern("^item-[0-9]+$")
string ItemId

// Enum with proper single-line syntax
enum Category {
    ELECTRONICS
    CLOTHING  
    BOOKS
    HOME
}

// IntEnum with values
intEnum Priority {
    LOW = 1
    MEDIUM = 2
    HIGH = 3
}

// Error structures
@error("client")
structure NotFoundError {
    @required
    message: String
    
    @required
    itemId: ItemId
}

@error("client")
structure ValidationError {
    @required
    message: String
    
    fieldErrors: FieldErrorList
}

list FieldErrorList {
    member: FieldError
}

structure FieldError {
    @required
    field: String
    
    @required
    message: String
}

map ItemMetadata {
    key: String
    value: String
}
