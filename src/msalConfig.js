export const msalConfig = {
  auth: {
    clientId: '<FRONTEND_APP_CLIENT_ID>',
    authority: 'https://login.microsoftonline.com/<TENANT_ID>',
    redirectUri: window.location.origin
  }
}

export const loginRequest = {
  scopes: ['api://<API_CLIENT_ID>/access_as_user']
}
