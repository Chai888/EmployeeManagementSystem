
import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import {
  EmployeeService,
  Employee
} from '../../../core/services/employee.service';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-employee-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './employee-list.html',
  styleUrl: './employee-list.css'
})
export class EmployeeList implements OnInit {
  employees: Employee[] = [];
  loading = true;
  errorMessage = '';
  successMessage = '';

  searchText = '';
  selectedDepartment = '';
  selectedStatus = 'all';

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

  constructor(
    private employeeService: EmployeeService,
    private authService: AuthService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  get departments(): string[] {
    return [
      ...new Set(
        this.employees
          .map(employee => employee.department?.trim())
          .filter((department): department is string => !!department)
      )
    ].sort((a, b) => a.localeCompare(b));
  }

  get filteredEmployees(): Employee[] {
    const search = this.searchText.trim().toLowerCase();

    return this.employees.filter(employee => {
      const matchesSearch =
        employee.employeeNo?.toLowerCase().includes(search) ||
        employee.name?.toLowerCase().includes(search) ||
        employee.email?.toLowerCase().includes(search);

      const matchesDepartment =
        !this.selectedDepartment ||
        employee.department?.trim() === this.selectedDepartment;

      const matchesStatus =
        this.selectedStatus === 'all' ||
        (this.selectedStatus === 'active' && employee.isActive) ||
        (this.selectedStatus === 'inactive' && !employee.isActive);

      return matchesSearch && matchesDepartment && matchesStatus;
    });
  }

  ngOnInit(): void {
    this.successMessage = history.state?.successMessage ?? '';
    this.loadEmployees();
  }

  loadEmployees(): void {
    this.loading = true;
    this.errorMessage = '';

    this.employeeService.getEmployees().subscribe({
      next: (data: Employee[]) => {
        this.employees = data;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Failed to load employees:', error);
        this.errorMessage = 'Failed to load employees.';
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  onLogout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
