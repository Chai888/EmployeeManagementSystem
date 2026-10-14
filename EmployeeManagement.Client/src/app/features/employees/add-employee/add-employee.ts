import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import {
  EmployeeService,
  CreateEmployee,
  CreateEmployeeResponse
} from '../../../core/services/employee.service';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-add-employee',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './add-employee.html',
  styleUrl: './add-employee.css'
})
export class AddEmployee implements OnInit {
  departments: { id: number; name: string }[] = [];

  newEmployee: CreateEmployee = {
    employeeNo: '',
    name: '',
    email: '',
    departmentId: 0,
    position: '',
    joinDate: new Date().toISOString().slice(0, 10),
    isActive: true
  };

  saving = false;
  createErrorMessage = '';

  // Stores the account details returned by the API.
  // Only shown immediately after successful creation.
  createdAccount: CreateEmployeeResponse | null = null;

  copyStatusMessage = '';

  constructor(
    private employeeService: EmployeeService,
    private authService: AuthService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.employeeService.getDepartments().subscribe({
      next: (data) => {
        this.departments = data;
      },
      error: (error) => {
        console.error('Failed to load departments:', error);
        this.createErrorMessage = 'Failed to load departments.';
      }
    });
  }

  onCreateEmployee(form: NgForm): void {
    if (form.invalid || this.saving) return;

    this.saving = true;
    this.createErrorMessage = '';
    this.copyStatusMessage = '';

    const payload: CreateEmployee = {
      ...this.newEmployee,
      departmentId: Number(this.newEmployee.departmentId),
      joinDate: new Date(this.newEmployee.joinDate).toISOString()
    };

    this.employeeService.createEmployee(payload).subscribe({
      next: (response) => {
        this.createdAccount = response;
        this.saving = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Failed to create employee:', error);

        this.saving = false;
        this.createErrorMessage =
          error.error?.message ||
          `Failed to create employee (HTTP ${error.status || 'unknown'}).`;

        this.cdr.detectChanges();
      }
    });
  }

  async copyText(value: string, label: string): Promise<void> {
    try {
      await navigator.clipboard.writeText(value);
      this.copyStatusMessage = `${label} copied successfully.`;
    } catch {
      this.copyStatusMessage =
        'Copy failed. Please select and copy the text manually.';
    }
  }

  onContinueToEmployees(): void {
    this.router.navigate(['/employees'], {
      state: {
        successMessage: 'Employee and login account created successfully.'
      }
    });
  }

  onCancel(): void {
    this.router.navigate(['/employees']);
  }

  onLogout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
