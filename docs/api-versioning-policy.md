# TMS API Versioning Policy

## Purpose

The TMS API uses versioning to allow the API contract to evolve without breaking existing clients. Changes are classified as either non-breaking additions or breaking changes that require a new API version.

## Breaking Changes

A change is considered breaking if an existing client may fail or behave differently after the change. Examples include:

* Removing an existing response field.
* Renaming an existing request or response field.
* Changing an HTTP status code that clients depend on.
* Tightening validation rules that reject previously valid requests.
* Changing default sorting or filtering behavior.
* Changing the meaning of an existing field.

Breaking changes require a new API version.

## Additive Changes

A change is considered non-breaking when existing clients continue to work without modification. Examples include:

* Adding a new optional response field.
* Adding a new endpoint.
* Adding a new optional query parameter.
* Adding additional optional metadata to responses.

Additive changes may be released within the current API version.

## Sunset Policy

When a new API version is released, the previous version remains available for a minimum of six months.

This six-month window allows clients, including rural training centres with quarterly maintenance schedules, enough time to test and deploy migrations.

Deprecated versions communicate their retirement date through:

* `Deprecation` header.
* `Sunset` header.
* `Link` header pointing to the successor API version.

## Communication Process

When a version is deprecated, the team will:

* Add deprecation, sunset, and successor-version headers from the first day of the new version.
* Add an entry to the CHANGELOG.
* Notify every team that owns an API key.
* Create a calendar invitation for the planned shutdown date.

## Version Skipping

Clients are not required to migrate through every intermediate API version.

For example, migration directly from V1 to V3 is allowed if V3 is the current supported version.

## Ownership

API changes must be reviewed against this policy before release. Every proposed change must clearly identify whether it is breaking or additive and whether a new version is required.

## Header-Based Versioning

Header-based versioning using `X-Api-Version` is supported as a partner-specific opt-in.

The default API versioning strategy remains URL segments because URLs make the active version visible during debugging, monitoring, and incident response.

Partners that cannot change cached URLs or CDN paths may request header-based versioning. The migration approach is agreed individually with each partner.

**Step 7 – When Not to Use HATEOAS**

HATEOAS should be used when the client and server are developed by different teams or when the API may evolve over time. In this case, clients can discover available resources and actions through links instead of relying on hardcoded URLs. For the TMS V2 API, HATEOAS is useful because it provides a stable contract for the Angular frontend and any future external integration partners.

HATEOAS is usually unnecessary when the client and server are owned by the same team and evolve together, such as internal service-to-service communication between TMS microservices. In these situations, the additional complexity of generating and maintaining hypermedia links provides little benefit.

HATEOAS should never replace API documentation. OpenAPI and Scalar remain essential for documenting endpoints, request and response models, authentication, and error handling. Hypermedia complements documentation by helping clients navigate the API at runtime.

For most production REST APIs, a small set of links is sufficient. Collection resources typically include `self`, `next`, and `prev`, while detail resources include `self` and one relevant action link. Additional links should only be added when there is a real client requirement.
