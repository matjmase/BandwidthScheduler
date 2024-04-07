import { inject } from '@angular/core';
import { HttpInterceptorFn } from '@angular/common/http';
import { BackendConnectService } from '../services/backend-connect.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const backend = inject(BackendConnectService);
  const idToken = backend.Login.GetSavedResponse()?.token;

  if (idToken) {
    const cloned = request.clone({
      headers: request.headers.set('Authorization', 'Bearer ' + idToken),
    });

    return next(cloned);
  } else {
    return next(request);
  }
};
