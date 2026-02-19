# VetClinic API – Frontend Integration Guide

This document describes the auth API the frontend must call for **registration**, **login**, and **logout**. All responses use **JSON** with **camelCase** property names.

---

## Base URL and environment

- **Base URL**: Provided by backend team (e.g. `https://<api-id>.execute-api.<region>.amazonaws.com/<stage>` or your custom domain).
- **Content type**: Send `Content-Type: application/json` for all request bodies.
- **CORS**: The API returns `Access-Control-Allow-Origin: *` and allows `Content-Type` and `Authorization` headers. Preflight **OPTIONS** requests are supported.

---

## 1. Registration

**Endpoint:** `POST /auth/register`

**Request body:**

| Field           | Type   | Required | Description                    |
|----------------|--------|----------|--------------------------------|
| firstName      | string | Yes      | User's first name              |
| lastName       | string | Yes      | User's last name               |
| email          | string | Yes      | Valid email (unique in system)  |
| phoneNumber    | string | No       | Phone number                   |
| password       | string | Yes      | Min 8 chars (see rules below)  |
| confirmPassword| string | Yes      | Must match `password`          |

**Password rules (enforced by backend):**

- Minimum 8 characters  
- At least one uppercase letter  
- At least one lowercase letter  
- At least one number  

**Example request:**

```json
{
  "firstName": "Jane",
  "lastName": "Doe",
  "email": "jane.doe@example.com",
  "phoneNumber": "+1234567890",
  "password": "SecurePass1",
  "confirmPassword": "SecurePass1"
}
```

**Success response:** `201 Created`

```json
{
  "userId": "a1b2c3d4-...",
  "email": "jane.doe@example.com",
  "role": "Client",
  "confirmationRequired": false,
  "message": "Registration successful"
}
```

- **userId**: Cognito user identifier (use for display or future API calls if needed).  
- **role**: One of `"Client"`, `"Veterinarian"`, `"Receptionist"`, `"Admin"` (assigned automatically; new users are `"Client"` unless on a role list).  
- **confirmationRequired**: If `true`, user may need to confirm email before full access (e.g. redirect to a “confirm your email” step).

**Error response:** `400 Bad Request`

```json
{
  "error": "Bad Request",
  "code": "INVALID_INPUT",
  "message": "Invalid input data"
}
```

Possible `message` values (non‑exhaustive):

- `"Invalid or missing request body."`  
- `"Invalid input data"` (validation failed)  
- `"An account with this email already exists."`  
- `"Password does not meet requirements (min 8 chars, upper, lower, number)."`  
- `"Registration failed."`  

**Frontend recommendations:**

- Validate **email format**, **password length**, and **password === confirmPassword** before sending.  
- On **201**: show confirmation message and redirect to **login** (e.g. “Account created. Please sign in.”).  
- On **400**: show `message` to the user.

---

## 2. Login

**Endpoint:** `POST /auth/login`

**Request body:**

| Field    | Type   | Required | Description   |
|----------|--------|----------|----------------|
| email    | string | Yes      | User's email   |
| password | string | Yes      | User's password|

**Example request:**

```json
{
  "email": "jane.doe@example.com",
  "password": "SecurePass1"
}
```

**Success response:** `200 OK`

```json
{
  "idToken": "eyJraWQ...",
  "accessToken": "eyJraWQ...",
  "refreshToken": "eyJjdH...",
  "expiresIn": 3600,
  "tokenType": "Bearer",
  "user": {
    "sub": "a1b2c3d4-...",
    "email": "jane.doe@example.com",
    "role": "Client"
  }
}
```

- **idToken**: Use for identity (e.g. decode for `sub` if needed).  
- **accessToken**: Send in `Authorization: Bearer <accessToken>` for protected APIs (when they exist).  
- **refreshToken**: Store securely; use to get new tokens when `accessToken` expires (refresh flow to be defined if backend adds a refresh endpoint).  
- **expiresIn**: Lifetime of `accessToken` in **seconds**.  
- **user**: Current user info; **role** is one of `"Client"`, `"Veterinarian"`, `"Receptionist"`, `"Admin"`.

**Error responses:**

- **400 Bad Request** – e.g. missing email/password:

```json
{
  "error": "Bad Request",
  "code": "INVALID_INPUT",
  "message": "Email and password are required."
}
```

- **401 Unauthorized** – wrong credentials:

```json
{
  "error": "Unauthorized",
  "code": "INVALID_CREDENTIALS",
  "message": "Invalid email or password."
}
```

**Frontend recommendations:**

- Store **accessToken** (and optionally **idToken**) in memory or secure storage; send **accessToken** in `Authorization: Bearer <accessToken>` for authenticated requests.  
- Store **user** (at least `sub`, `email`, `role`) for UI (e.g. header, role-based menus).  
- On **200**: redirect to **main page**; consider persisting login (e.g. localStorage/sessionStorage) so the user “remains logged in across sessions.”  
- On **401**: show “Invalid email or password” (or use `message`).

---

## 3. Logout

**Endpoint:** `POST /auth/logout`

No body required if the access token is sent in the header.

**Option A – Header (recommended):**

```
Authorization: Bearer <accessToken>
```

**Option B – Body:**

```json
{
  "accessToken": "<accessToken>"
}
```

**Success response:** `200 OK`

```json
{
  "message": "Logged out successfully."
}
```

**Frontend recommendations:**

- Clear stored tokens and user info.  
- Redirect to **login** page.

---

## Error response shape (all errors)

All error responses use this structure (when the body is JSON):

| Field     | Type   | Description        |
|----------|--------|--------------------|
| error    | string | Short label        |
| code     | string | Machine-readable   |
| message  | string | Human-readable     |

**Common error codes:**

| Code                 | Typical HTTP | Meaning                    |
|----------------------|-------------|----------------------------|
| INVALID_INPUT        | 400         | Validation / bad request  |
| INVALID_CREDENTIALS  | 401         | Wrong email or password   |
| EMAIL_ALREADY_EXISTS| 400         | Email already registered  |
| WEAK_PASSWORD        | 400         | Password too weak         |
| PASSWORD_MISMATCH    | 400         | Password ≠ confirmPassword|

**Other status codes:**

- **404** – Path not found.  
- **405** – Method not allowed (e.g. GET on a POST-only path).  
- **500** – Server error; body may contain `{ "error", "message" }`.

---

## User roles

Used in **RegisterResponse** and **LoginResponse.user.role**:

| Role          | Description (for UI)        |
|---------------|-----------------------------|
| Client        | Default for new users      |
| Veterinarian  | Assigned from role list     |
| Receptionist  | Assigned from role list     |
| Admin         | Assigned from role list     |

Use **role** to show/hide features or redirect after login (e.g. different home for Client vs Admin).

---

## Quick reference

| Action   | Method | Path             | Auth header   |
|----------|--------|------------------|---------------|
| Register | POST   | `/auth/register` | Not required  |
| Login    | POST   | `/auth/login`    | Not required  |
| Logout   | POST   | `/auth/logout`   | Bearer token  |

---

## Checklist for frontend

- [ ] Base URL and stage (dev/staging/prod) from backend team.  
- [ ] Registration form: firstName, lastName, email, phoneNumber, password, confirmPassword.  
- [ ] Client-side validation: email format, password length and match, required fields.  
- [ ] On successful registration: show message, redirect to login.  
- [ ] Login: send email + password, store tokens and `user` (sub, email, role).  
- [ ] Send `Authorization: Bearer <accessToken>` for logout and any future protected endpoints.  
- [ ] On logout: clear tokens and user, redirect to login.  
- [ ] Handle 400/401 and display `message` (and optionally `code`) to the user.  
- [ ] Use `user.role` for role-based UI or routing.
