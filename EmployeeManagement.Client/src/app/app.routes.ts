
import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth-guard';
import { roleGuard } from './core/guards/role-guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login/login')
        .then(m => m.Login)
  },
  {
    path: 'admin-dashboard',
    loadComponent: () =>
      import('./features/admin/admin-dashboard/admin-dashboard')
        .then(m => m.AdminDashboard),
    canActivate: [authGuard, roleGuard],
    data: { role: 'Admin' }
  },
  {
    path: 'employees',
    loadComponent: () =>
      import('./features/employees/employee-list/employee-list')
        .then(m => m.EmployeeList),
    canActivate: [authGuard]
  },
  {
    path: 'employees/new',
    loadComponent: () =>
      import('./features/employees/add-employee/add-employee')
        .then(m => m.AddEmployee),
    canActivate: [authGuard]
  },
  {
    path: 'employees/:id/edit',
    loadComponent: () =>
      import('./features/employees/employee-edit/employee-edit')
        .then(m => m.EmployeeEdit),
    canActivate: [authGuard]
  },
  {
    path: 'employees/:id',
    loadComponent: () =>
      import('./features/employees/employee-details/employee-details')
        .then(m => m.EmployeeDetails),
    canActivate: [authGuard]
  },
  {
    path: 'departments',
    loadComponent: () =>
      import('./features/departments/department-list/department-list')
        .then(m => m.DepartmentList),
    canActivate: [authGuard]
  },
  {
    path: 'public-holidays',
    loadComponent: () =>
      import('./features/public-holidays/public-holiday-list/public-holiday-list')
        .then(m => m.PublicHolidayList),
    canActivate: [authGuard, roleGuard],
    data: { role: 'Admin' }
  },
  {
    path: 'leave',
    loadComponent: () =>
      import('./features/leave/leave-dashboard/leave-dashboard')
        .then(m => m.LeaveDashboard),
    canActivate: [authGuard, roleGuard],
    data: { role: 'Employee' }
  },
  {
    path: 'leave/apply',
    loadComponent: () =>
      import('./features/leave/apply-leave/apply-leave')
        .then(m => m.ApplyLeave),
    canActivate: [authGuard, roleGuard],
    data: { role: 'Employee' }
  },
  {
    path: 'leave-approval',
    loadComponent: () =>
      import('./features/leave/leave-approval/leave-approval')
        .then(m => m.LeaveApproval),
    canActivate: [authGuard, roleGuard],
    data: { role: 'Admin' }
  },
  {
    path: 'change-password',
    loadComponent: () =>
      import('./features/auth/change-password/change-password')
        .then(m => m.ChangePassword),
    canActivate: [authGuard]
  },
  {
    path: 'leave-entitlement',
    loadComponent: () =>
      import('./features/leave/leave-entitlement/leave-entitlement')
        .then(m => m.LeaveEntitlementPage),
    canActivate: [authGuard, roleGuard],
    data: { role: 'Admin' }
  },
  {
    path: 'leave-history',
    loadComponent: () =>
      import('./features/leave/leave-history/leave-history')
        .then(m => m.LeaveHistory),
    canActivate: [authGuard, roleGuard],
    data: { role: 'Admin' }
  },
  {
    path: '',
    redirectTo: 'login',
    pathMatch: 'full'
  },
  {
    path: '**',
    redirectTo: 'login'
  }
];
