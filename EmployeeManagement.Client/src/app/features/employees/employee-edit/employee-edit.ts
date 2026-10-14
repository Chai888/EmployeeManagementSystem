
import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { EmployeeService, Employee, CreateEmployee, Department } from '../../../core/services/employee.service';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-employee-edit',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './employee-edit.html',
  styleUrl: './employee-edit.css'
})
export class EmployeeEdit implements OnInit {
  private cdr = inject(ChangeDetectorRef);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private employeeService = inject(EmployeeService);
  private authService = inject(AuthService);

  employeeId = 0;
  loading = true;
  saving = false;
  errorMessage = '';
  successMessage = '';
  departments: Department[] = [];

  employee: CreateEmployee = {
    employeeNo: '',
    name: '',
    email: '',
    departmentId: 0,
    position: '',
    joinDate: '',
    isActive: true
  };

  ngOnInit(): void {
    this.employeeId = Number(this.route.snapshot.paramMap.get('id'));

    if (!Number.isInteger(this.employeeId) || this.employeeId <= 0) {
      this.errorMessage = 'Invalid employee ID.';
      this.loading = false;
	  this.cdr.detectChanges();
      return;
    }

	this.employeeService.getDepartments().subscribe({
		next: (departments) => {
		  this.departments = departments;
	  
		this.employeeService.getEmployeeById(this.employeeId).subscribe({
		  next: (data: Employee) => {
			const existingDepartment = this.departments.find(
			  d => d.name.trim().toLowerCase() ===
                   data.department.trim().toLowerCase()
			);

			this.employee = {
			  employeeNo: data.employeeNo,
			  name: data.name,
			  email: data.email,
			  // GET currently returns department name, not its ID.
			  // We must obtain the real ID before saving.
			  departmentId: existingDepartment?.id ?? 0,
			  position: data.position,
			  joinDate: data.joinDate.substring(0, 10),
			  isActive: data.isActive
			};

			this.loading = false;
			this.cdr.detectChanges();
			
			if (!existingDepartment) {
              this.errorMessage =
                'Could not match this employee to a department. Please check the department names returned by the API.';
            }
		  },
		  error: (error) => {
			console.error('Failed to load employee:', error);
			this.errorMessage = 'Failed to load employee for editing.';
			this.loading = false;
			this.cdr.detectChanges();
		  }
		});
	  },
	  error: (error) => {
      console.error('Failed to load departments:', error);
      this.errorMessage = 'Failed to load departments.';
      this.loading = false;
	  this.cdr.detectChanges();
    }
  });
}

  onUpdateEmployee(form: NgForm): void {
    if (form.invalid || this.saving) {
      return;
    }

    if (this.employee.departmentId <= 0) {
      this.errorMessage = 'Please select a valid department.';
      return;
    }

    this.saving = true;
    this.errorMessage = '';

    const payload: CreateEmployee = {
      ...this.employee,
      departmentId: Number(this.employee.departmentId),
      joinDate: new Date(
        `${this.employee.joinDate}T00:00:00`
      ).toISOString()
    };

    this.employeeService.updateEmployee(this.employeeId, payload).subscribe({
      next: () => {
	    this.saving = false;
	  
	    this.router.navigate(['/employees', this.employeeId], {
	  	state: {
	  	  successMessage: 'Employee updated successfully.'
	  	}
	    });
	  },
      error: (error) => {
        console.error('Failed to update employee:', error);
        this.saving = false;
        this.errorMessage =
          error.error?.message ||
          `Failed to update employee (HTTP ${error.status || 'unknown'}).`;
      }
    });
  }
  
  cancel(): void {
    this.router.navigate(['/employees', this.employeeId]);
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
