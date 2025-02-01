 # Expense Tracker API

 ![image](https://github.com/user-attachments/assets/6f5801e1-40bf-4944-b945-d9ca3bf19ec1)

## Stack and tech used
- dotnet 8
- postgresql (supabase)
- ef orm
- mediatr
- carter

## Features
- Sign up using /api/users
- Authenticate using /api/authenticate
  - This returns an access token and a refresh token
  - Access token expires in 20 min, refresh token expires in 30 days
- Refresh token using /api/authenticate/refresh
  - This returns a new access token and a new refresh token
  - This will invalidate the old refresh token
- Add expense with POST /api/expenses
- Update expense with PATCH /api/expenses/{id}
  - Only include the properties you want to update
  - If you use the "Other" expense type, then you need to include a name
- Delete expense with DELETE /api/expenses/{id}
- Search expenses with GET /api/expenses
  - Filters include 7 day, 30 month, 90 day, and custom
  - Using custom requires a starting and ending date filter

Project from https://roadmap.sh/projects/expense-tracker-api
