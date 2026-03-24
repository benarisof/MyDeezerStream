import { ApplicationConfig } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { routes } from './app.routes';
import { provideAuth0, authHttpInterceptorFn } from '@auth0/auth0-angular'; 
import { logInterceptor } from './log.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(
      withInterceptors([
        authHttpInterceptorFn, 
        logInterceptor 
      ])
    ),
    provideAuth0({
      domain: 'dev-gycl8oiljwzvyln5.us.auth0.com',
      clientId: 'cnICIlropjVm2gquexLCKZIErITVOoZ1',
      authorizationParams: {
        redirect_uri: window.location.origin,
        audience: 'https://mydeezer.api',
        scope: 'openid profile email'
      },
      httpInterceptor: {
        allowedList: [
          {
            uri: 'http://localhost:5257/api/*',
            tokenOptions: {
              authorizationParams: {
                audience: 'https://mydeezer.api',
              }
            }
          }
        ]
      }
    }),
  ]
};