export const msalConfig = {
  auth: {
    clientId: '<FRONTEND_APP_CLIENT_ID>', // Move Front End App Client ID to Environment Variables
    authority: 'https://login.microsoftonline.com/a9045576-359a-419e-a24b-3b0ed64b6b64', // Move Tenant ID to Environment Variables
    redirectUri: window.location.origin
  }
}

export const loginRequest = {
  scopes: ['api://ceb1d1cf-b928-4749-94e2-f84cfcebc593/access_as_user'] // Move Scope URI to environment variables
}
