import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  if (auth.isLoggedIn()) return true;
  inject(Router).navigate(['/login']);
  return false;
};

export const adminGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  if (auth.isLoggedIn() && auth.getRole() === 'Admin') return true;
  inject(Router).navigate([auth.isLoggedIn() ? '/user' : '/login']);
  return false;
};

export const userGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  if (auth.isLoggedIn() && auth.getRole() === 'User') return true;
  inject(Router).navigate([auth.isLoggedIn() ? '/admin/flights' : '/login']);
  return false;
};
