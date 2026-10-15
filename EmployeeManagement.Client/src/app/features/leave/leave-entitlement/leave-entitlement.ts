
import { CommonModule } from '@angular/common';
import {
  Component,
  ChangeDetectorRef,
  inject
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { EmployeeService, Employee } from '../../../core/services/employee.service';
import { AuthService } from '../../../core/services/auth.service';
import {
  LeaveEntitlement,
  LeaveEntitlementService
} from '../../../core/services/leave-entitlement.service';

@Component({
  selector: 'app-leave-entitlement',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './leave-entitlement.html',
  styleUrl: './leave-entitlement.css'
})
export class LeaveEntitlementPage {
  private employeeService = inject(EmployeeService);
  private entitlementService = inject(LeaveEntitlementService);
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  employees: Employee[] = [];
  entitlements: LeaveEntitlement[] = [];

  selectedEmployeeId: number | null = null;
  year = new Date().getFullYear();
  editingEntitlementId: number | null = null;

  leaveTypes = [
    'Annual Leave',
    'Medical Leave',
    'Hospitalisation Leave',
    'Maternity Leave',
    'Paternity Leave',
    'Compassionate Leave',
    'Marriage Leave',
    'Replacement Leave',
    'Unpaid Leave',
    'Other'
  ];

  leaveType = 'Annual Leave';
  customLeaveType = '';
  entitledDays = 0;

  loadingEmployees = false;
  loading = false;
  saving = false;

  errorMessage = '';
  successMessage = '';

  constructor(private cdr: ChangeDetectorRef) {
    this.loadEmployees();
  }

  loadEmployees(): void {
    this.loadingEmployees = true;
    this.errorMessage = '';

    this.employeeService.getEmployees().subscribe({

      next: (employees) => {
        this.employees = employees;
        this.loadingEmployees = false;

        // Read the employee ID passed from Employee Details
        const employeeIdParam =
          this.route.snapshot.queryParamMap.get('employeeId');

        if (employeeIdParam !== null) {
          const employeeId = Number(employeeIdParam);

          // Auto-select only if the employee exists in the list
          if (
            Number.isInteger(employeeId) &&
            this.employees.some(emp => emp.id === employeeId)
          ) {
            this.selectedEmployeeId = employeeId;
            this.loadEntitlements();
          }
        }

        this.cdr.detectChanges();
      },
      error: (error: HttpErrorResponse) => {
        this.loadingEmployees = false;
        this.errorMessage = this.getErrorMessage(
          error,
          'Unable to load employees. Please try again.'
        );
        this.cdr.detectChanges();
      }
    });
  }

  onEmployeeChange(): void {
    this.successMessage = '';
    this.errorMessage = '';
    this.entitlements = [];
    this.resetForm();

    this.loadEntitlements();
  }

  onYearChange(): void {
    this.successMessage = '';
    this.errorMessage = '';
    this.entitlements = [];

    if (!this.isValidYear()) {
      this.errorMessage = 'Please enter a valid year between 2000 and 2100.';
      return;
    }

    this.loadEntitlements();
  }

  private isValidYear(): boolean {
    return Number.isInteger(Number(this.year)) &&
      Number(this.year) >= 2000 &&
      Number(this.year) <= 2100;
  }

  loadEntitlements(): void {
    if (
      this.selectedEmployeeId === null ||
      !this.isValidYear()
    ) {
      return;
    }

    this.loading = true;
    this.errorMessage = '';

    this.entitlementService
      .getForEmployee(this.selectedEmployeeId, Number(this.year))
      .subscribe({
        next: (result) => {
          this.entitlements = result;
          this.loading = false;
          this.cdr.detectChanges();
        },
        error: (error: HttpErrorResponse) => {
          this.entitlements = [];
          this.loading = false;
          this.errorMessage = this.getErrorMessage(
            error,
            'Unable to load leave entitlements.'
          );
          this.cdr.detectChanges();
        }
      });
  }

  editEntitlement(item: LeaveEntitlement): void {
    this.editingEntitlementId = item.id;

    this.leaveType = this.leaveTypes.includes(item.leaveType)
      ? item.leaveType
      : 'Other';

    this.customLeaveType =
      this.leaveType === 'Other' ? item.leaveType : '';

    this.entitledDays = item.entitledDays;
    this.errorMessage = '';
    this.successMessage = '';

    // Scroll to the form so the user can edit the selected record.
    document.querySelector('.entitlement-form')
      ?.scrollIntoView({ behavior: 'smooth', block: 'center' });
  }

  saveEntitlement(): void {
    if (this.selectedEmployeeId === null) {
      this.errorMessage = 'Please select an employee.';
      return;
    }

    if (!this.isValidYear()) {
      this.errorMessage = 'Please enter a valid leave year.';
      return;
    }

    const actualLeaveType =
      this.leaveType === 'Other'
        ? this.customLeaveType.trim()
        : this.leaveType;

    if (!actualLeaveType) {
      this.errorMessage = 'Please enter a leave type.';
      return;
    }

    if (
      !Number.isFinite(Number(this.entitledDays)) ||
      Number(this.entitledDays) < 0 ||
      Number(this.entitledDays) > 365
    ) {
      this.errorMessage =
        'Entitlement must be between 0 and 365 days.';
      return;
    }

    this.saving = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.entitlementService.save(this.selectedEmployeeId, {
      year: Number(this.year),
      leaveType: actualLeaveType,
      entitledDays: Number(this.entitledDays)
    }).subscribe({
      next: () => {
        this.saving = false;
        this.successMessage = this.editingEntitlementId !== null
          ? 'Leave entitlement updated successfully.'
          : 'Leave entitlement saved successfully.';

        this.editingEntitlementId = null;
        this.resetForm();
        this.loadEntitlements();
        this.cdr.detectChanges();
      },
      error: (error: HttpErrorResponse) => {
        this.saving = false;
        this.errorMessage = this.getErrorMessage(
          error,
          'Unable to save leave entitlement.'
        );
        this.cdr.detectChanges();
      }
    });
  }

  resetForm(): void {
    this.editingEntitlementId = null;
    this.leaveType = 'Annual Leave';
    this.customLeaveType = '';
    this.entitledDays = 0;
  }

  closeErrorMessage(): void {
    this.errorMessage = '';
  }

  closeSuccessMessage(): void {
    this.successMessage = '';
  }

  goBack(): void {
    this.router.navigate(['/admin-dashboard']);
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  private getErrorMessage(
    error: HttpErrorResponse,
    fallback: string
  ): string {
    if (typeof error.error === 'string' && error.error.trim()) {
      return error.error;
    }

    if (error.error?.message) {
      return error.error.message;
    }

    if (error.error?.title) {
      return error.error.title;
    }

    return fallback;
  }
}
