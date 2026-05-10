export const environment = {
  production: true,
  backendUrl: 'https://YOUR_PROD_DOMAIN',
  signalRUrl: 'https://YOUR_PROD_DOMAIN',
  oauth: {
    clientId: 'ewallet-client',
    authorizationEndpoint: 'https://YOUR_PROD_DOMAIN/connect/authorize',
    tokenEndpoint: 'https://YOUR_PROD_DOMAIN/connect/token',
    redirectUri: 'https://YOUR_PROD_DOMAIN/auth/callback',
    scopes: 'openid profile email offline_access wallet',
  },
};
