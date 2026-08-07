# Original assignment brief

Preserved verbatim from the README of the upstream repository, so the submission
can be read against exactly what was asked.

---

# C# Accepted Assessment app

An application for C# (.net) knowledge assessment

## Description

This is a web application that interacts with a 3nd party service (<https://fakeapi.platzi.com>/<https://api.escuelajs.co>) and serves data

We have to do some code refactoring and implement some new features

## Code refactoring

Seems that the use of http client is not so much efficient

Let's make a different, more solid, approach/implementation

## New features

**#1**

Right now only the **getAll** method supported for **products**

We have to implement **getOne** and **create** methods also

**#2**

Add implementation for **categories**

**#3**

3nd party service supports JWT auth. We have to implement and support it. Use the credentials provided to appsettings.json file.

**#4**

We must measure and log the performance of the requests. Create a middleware to achieve this.

## Implementation

* Try to understand and keep the architectural approach.
* Add unit testing.
* Add docker support.
* Using CQRS pattern will be considered as a strong plus.
* The attached collections (postman/insomnia) will help you with the requests.

---

## How this submission addresses it

| Requirement | Status |
| --- | --- |
| HTTP client refactoring | Done — typed clients via `IHttpClientFactory` with a Polly v8 resilience pipeline |
| #1 products `getOne` and `create` | Done, plus `update` and `delete` |
| #2 categories | Done — full CRUD |
| #3 JWT auth | Done — cached, refreshed, attached by a `DelegatingHandler` |
| #4 performance middleware | Done — structured timing, slow-request escalation, correlation ids |
| Keep the architectural approach | Kept — the four-project layering is unchanged |
| Unit testing | Done — 79 tests |
| Docker support | **Not done** |
| CQRS | **Not adopted** — see "Decisions and trade-offs" in the README |
