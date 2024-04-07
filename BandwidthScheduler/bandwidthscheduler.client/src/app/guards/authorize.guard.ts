import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { BackendConnectService } from '../services/backend-connect.service';

export const authorizeGuard: CanActivateFn = (route, state) => {
  const backend = inject(BackendConnectService);
  const router = inject(Router);

  const acceptedRoles = route.data['roles'] as Array<string>;

  const currentRoles = backend.Login.GetSavedResponse()?.roles;

  let result = false;
  if (currentRoles && acceptedRoles && currentRoles) {
    result = acceptedRoles.some((e) => currentRoles.some((f) => f === e));
  } else {
    result = false;
  }

  if (!result) {
    router.navigate(['']);
  }

  return result;
};
