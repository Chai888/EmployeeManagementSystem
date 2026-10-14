
import {
  Component,
  OnInit,
  ChangeDetectorRef,
  inject
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import {
  EmployeeService,
  Department,
  CreateDepartment
} from '../../../core/services/employee.service';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-department-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './department-list.html',
  styleUrl: './department-list.css'
})
export class DepartmentList implements OnInit {
  private employeeService = inject(EmployeeService);
  private authService = inject(AuthService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  departments: Department[] = [];

  loading = true;
  saving = false;
  deletingId: number | null = null;

  showForm = false;
  editingId: number | null = null;

  departmentName = '';

  successMessage = '';
  errorMessage = '';
  showSuccessMessage = true;
  showErrorMessage = true;

  ngOnInit(): void {
    this.loadDepartments();
  }

  loadDepartments(): void {
    this.loading = true;
    this.errorMessage = '';
    this.showErrorMessage = true;

    this.employeeService.getDepartments().subscribe({
      next: (departments) => {
        this.departments = departments;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Failed to load departments:', error);
        this.loading = false;
        this.errorMessage =
          error.status === 401 || error.status === 403
            ? 'You do not have permission to view departments.'
            : 'Failed to load departments. Please try again.';
        this.showErrorMessage = true;
        this.cdr.detectChanges();
      }
    });
  }

  openAddForm(): void {
    this.editingId = null;
    this.departmentName = '';
    this.successMessage = '';
    this.errorMessage = '';
    this.showForm = true;
  }

  openEditForm(department: Department): void {
    this.editingId = department.id;
    this.departmentName = department.name;
    this.successMessage = '';
    this.errorMessage = '';
    this.showForm = true;
  }

  cancelForm(): void {
    this.showForm = false;
    this.editingId = null;
    this.departmentName = '';
    this.saving = false;
    this.errorMessage = '';
  }

  saveDepartment(): void {
    const name = this.departmentName.trim();

    this.errorMessage = '';
    this.showErrorMessage = true;

    if (!name) {
      this.errorMessage = 'Department name is required.';
      return;
    }

    if (this.saving) {
      return;
    }

    const payload: CreateDepartment = { name };
    this.saving = true;

    const request = this.editingId === null
      ? this.employeeService.createDepartment(payload)
      : this.employeeService.updateDepartment(
        this.editingId,
        payload
      );

    request.subscribe({
      next: () => {
        const isEditing = this.editingId !== null;

        this.saving = false;
        this.showForm = false;
        this.editingId = null;
        this.departmentName = '';

        this.successMessage = isEditing
          ? 'Department updated successfully.'
          : 'Department created successfully.';

        this.showSuccessMessage = true;
        this.loadDepartments();
      },
      error: (error) => {
        console.error('Failed to save department:', error);
        this.saving = false;

        this.errorMessage =
          error.error?.message ||
          (error.status === 401 || error.status === 403
            ? 'You need an Admin account to manage departments.'
            : error.status === 0
              ? 'Cannot connect to the server.'
              : `Failed to save department (HTTP ${error.status}).`);

        this.showErrorMessage = true;
        this.cdr.detectChanges();
      }
    });
  }

  deleteDepartment(department: Department): void {
    if (this.deletingId !== null) {
      return;
    }

    const confirmed = window.confirm(
      `Are you sure you want to delete "${department.name}"?`
    );

    if (!confirmed) {
      return;
    }

    this.errorMessage = '';
    this.showErrorMessage = true;
    this.deletingId = department.id;

    this.employeeService.deleteDepartment(department.id).subscribe({
      next: () => {
        this.deletingId = null;
        this.successMessage =
          'Department deleted successfully.';
        this.showSuccessMessage = true;
        this.loadDepartments();
      },
      error: (error) => {
        console.error('Failed to delete department:', error);
        this.deletingId = null;

        this.errorMessage =
          error.error?.message ||
          (error.status === 401 || error.status === 403
            ? 'You need an Admin account to delete departments.'
            : error.status === 0
              ? 'Cannot connect to the server.'
              : `Failed to delete department (HTTP ${error.status}).`);

        this.showErrorMessage = true;
        this.cdr.detectChanges();
      }
    });
  }

  closeSuccessMessage(): void {
    this.showSuccessMessage = false;
  }

  closeErrorMessage(): void {
    this.showErrorMessage = false;
  }

  goToEmployees(): void {
    this.router.navigate(['/employees']);
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
