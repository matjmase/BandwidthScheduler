import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { BackendConnectService } from '../services/backend-connect.service';

export const authenticateGuard: CanActivateFn = (route, state) => {
  const router = inject(Router);
  const backend = inject(BackendConnectService);
  const flip = route.data['flip'] as boolean;

  const loginState = backend.Login.GetSavedResponse();

  const loggedIn = loginState !== undefined;

  const result = !flip ? loggedIn : !loggedIn;

  if (!result) {
    router.navigate(['']);
  }

  return result;
};
