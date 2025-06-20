$version: "2"

namespace smithy.example

use aws.protocols#restJson1

@restJson1
service BeerService {
    operations: [
        GetBeer
        AddBeer
    ]
}

@http(method: "POST", uri: "/get-beer")
operation GetBeer {
    input := {
        id: Long
    }
    output := {
        beer: Beer
    }
}

structure Beer {
    @length(min: 3)
    name: String

    quantity: Long
}

@http(method: "POST", uri: "/add-beer")
operation AddBeer {
    input := {
        @required
        beer: Beer
    }

    output := {
        id: Long
    }
}