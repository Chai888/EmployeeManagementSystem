
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const roleGuard: CanActivateFn = (route) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const requiredRole = route.data['role'] as string;
  const userRole = authService.getUserRole()?.toLowerCase();

  if (!authService.isLoggedIn()) {
    return router.createUrlTree(['/login']);
  }

  if (userRole === requiredRole.toLowerCase()) {
    return true;
  }

  if (userRole === 'admin') {
    return router.createUrlTree(['/admin-dashboard']);
  }

  if (userRole === 'employee') {
    return router.createUrlTree(['/leave']);
  }

  authService.logout();
  return router.createUrlTree(['/login']);
};
