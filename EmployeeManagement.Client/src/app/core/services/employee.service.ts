
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface Employee {
  id: number;
  employeeNo: string;
  name: string;
  email: string;
  department: string;
  position: string;
  joinDate: string;
  isActive: boolean;
}

export interface CreateEmployee {
  employeeNo: string;
  name: string;
  email: string;
  departmentId: number;
  position: string;
  joinDate: string;
  isActive: boolean;
}

export interface CreateEmployeeResponse {
  id: number;
  employeeNo: string;
  name: string;
  email: string;
  department: string;
  position: string;
  joinDate: string;
  isActive: boolean;
  username: string;
  temporaryPassword: string;
}

export interface Department {
  id: number;
  name: string;
  employeeCount: number;
}

export interface CreateDepartment {
  name: string;
}

@Injectable({
  providedIn: 'root'
})
export class EmployeeService {
  private http = inject(HttpClient);

  private readonly apiUrl = `${environment.apiUrl}/Employee`;

  getEmployees(): Observable<Employee[]> {
    return this.http.get<Employee[]>(this.apiUrl);
  }
  
  getEmployeeById(id: number): Observable<Employee> {
	  return this.http.get<Employee>(`${this.apiUrl}/${id}`);
	}

  createEmployee(
    employee: CreateEmployee
  ): Observable<CreateEmployeeResponse> {
    return this.http.post<CreateEmployeeResponse>(
      this.apiUrl,
      employee
    );
  }
  
  updateEmployee(id: number, employee: CreateEmployee): Observable<Employee> {
    return this.http.put<Employee>(`${this.apiUrl}/${id}`, employee);
  }
  
  
  deleteEmployee(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  getDepartments(): Observable<Department[]> {
    return this.http.get<Department[]>(`${environment.apiUrl}/Department`);
  }

  createDepartment(
    department: CreateDepartment
  ): Observable<Department> {
    return this.http.post<Department>(
      `${environment.apiUrl}/Department`,
      department
    );
  }

  updateDepartment(
    id: number,
    department: CreateDepartment
  ): Observable<Department> {
    return this.http.put<Department>(
      `${environment.apiUrl}/Department/${id}`,
      department
    );
  }

  deleteDepartment(id: number): Observable<void> {
    return this.http.delete<void>(
      `${environment.apiUrl}/Department/${id}`
    );
  }
}
