# Proof of Concept for Authorization using C# and Swagger

## Building
Copy the Repository and save it the desired location on your PC
Make sure to run using https:
```bash
Use the button in your C# IDE to run without debugging(or with)
```
Go to the REST API:
```bash
https://localhost:7000/swagger/index.html
```

## Usage
```C#
# Locate the types of users
Go to DbSeeder.cs and pick a user.

# Guest: GET Articles
# Subscriber: POST Comment on Articles
# Writer: POST Articles and DELETE own Articles
# Editor: PUT and DELETE Comments and Articles
Go to Login and enter your user type as the email and insert the password.
Execute.
Copy the revealed accessToken
Go up in top right corner in Authorize and insert your token.
Explore your Access
```
