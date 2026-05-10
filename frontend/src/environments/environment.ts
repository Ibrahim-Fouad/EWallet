export const environment = {
  production: false,
  backendUrl: 'https://localhost:7000',
  signalRUrl: 'https://localhost:7000',
  oauth: {
    clientId: 'ewallet-client',
    authorizationEndpoint: 'https://localhost:7000/connect/authorize',
    tokenEndpoint: 'https://localhost:7000/connect/token',
    redirectUri: 'http://localhost:4200/auth/callback',
    scopes: 'openid profile email offline_access wallet',
  },
};
