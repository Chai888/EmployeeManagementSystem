
import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { EmployeeService, Employee } from '../../../core/services/employee.service';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-employee-details',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './employee-details.html',
  styleUrl: './employee-details.css'
})
export class EmployeeDetails implements OnInit {
  private cdr = inject(ChangeDetectorRef);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private employeeService = inject(EmployeeService);
  private authService = inject(AuthService);

  employee: Employee | null = null;
  loading = true;
  errorMessage = '';
  deleting = false;
  deleteError = '';
  successMessage = '';
  showSuccessMessage = true;
  showWarningMessage = true;
  showErrorMessage = true;
  
  closeSuccessMessage(): void {
    this.showSuccessMessage = false;
  }
  
  closeWarningMessage(): void {
    this.showWarningMessage = false;
  }
  
  closeErrorMessage(): void {
    this.showErrorMessage = false;
  }

  ngOnInit(): void {
	this.successMessage = history.state?.successMessage ?? '';
	
    const id = Number(this.route.snapshot.paramMap.get('id'));

    if (!Number.isInteger(id) || id <= 0) {
      this.errorMessage = 'Invalid employee ID.';
      this.loading = false;
      return;
    }

    this.employeeService.getEmployeeById(id).subscribe({
      next: (data) => {
        this.employee = data;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Failed to load employee:', error);

        this.errorMessage =
          error.status === 404
            ? 'Employee not found.'
            : 'Failed to load employee details.';

        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  deleteEmployee(): void {
    if (!this.employee || this.deleting) {
      return;
    }

    const confirmed = window.confirm(
      `Are you sure you want to delete ${this.employee.name}?`
    );

    if (!confirmed) {
      return;
    }

    this.deleting = true;
    this.deleteError = '';

    this.employeeService.deleteEmployee(this.employee.id).subscribe({
      next: () => {
        this.router.navigate(['/employees'], {
          state: { successMessage: 'Employee deleted successfully.' }
        });
      },
      error: (error) => {
        console.error('Failed to delete employee:', error);

        this.deleteError =
          error.status === 403
            ? 'You do not have permission to delete employees.'
            : error.status === 404
              ? 'Employee not found.'
              : 'Failed to delete employee. Please try again.';

        this.deleting = false;
        this.cdr.detectChanges();
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/employees']);
  }

  onLogout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  get isAdmin(): boolean {
    return this.authService.isAdmin();
  }

}
