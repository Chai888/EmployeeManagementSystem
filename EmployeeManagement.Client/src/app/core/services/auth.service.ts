
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

export interface LoginResponse {
  token: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);

  private readonly apiUrl = 'https://localhost:7148/api/Auth';

  login(
    username: string,
    password: string
  ): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(
      `${this.apiUrl}/login`,
      { username, password }
    ).pipe(
      tap(response => {
        localStorage.setItem('authToken', response.token);
      })
    );
  }

  getToken(): string | null {
    return localStorage.getItem('authToken');
  }

  logout(): void {
    localStorage.removeItem('authToken');
  }

  isLoggedIn(): boolean {
    return !!this.getToken();
  }


  changePassword(
    currentPassword: string,
    newPassword: string,
    confirmNewPassword: string
  ): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(
      `${this.apiUrl}/change-password`,
      {
        currentPassword,
        newPassword,
        confirmNewPassword
      }
    );
  }

  getUserRole(): string | null {
    const token = this.getToken();

    if (!token) {
      return null;
    }

    try {
      const payloadPart = token.split('.')[1];

      if (!payloadPart) {
        return null;
      }

      const base64 = payloadPart
        .replace(/-/g, '+')
        .replace(/_/g, '/');

      const payload = JSON.parse(atob(base64));

      const role =
        payload['role'] ??
        payload['roles'] ??
        payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];

      if (Array.isArray(role)) {
        return role.find(
          (item: string) => item.toLowerCase() === 'admin'
        ) ?? role[0] ?? null;
      }

      return typeof role === 'string' ? role : null;
    } catch {
      return null;
    }
  }

  getUsername(): string | null {
    const token = this.getToken();

    if (!token) {
      return null;
    }

    try {
      const payloadPart = token.split('.')[1];

      if (!payloadPart) {
        return null;
      }

      const base64 = payloadPart
        .replace(/-/g, '+')
        .replace(/_/g, '/');

      const payload = JSON.parse(atob(base64));

      return (
        payload['username'] ??
        payload['unique_name'] ??
        payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] ??
        payload['email'] ??
        null
      );
    } catch (error) {
      console.error('Failed to read username from token:', error);
      return null;
    }
  }

  getEmployeeId(): number | null {
    const token = this.getToken();

    if (!token) {
      return null;
    }

    try {
      const payloadPart = token.split('.')[1];

      if (!payloadPart) {
        return null;
      }

      const base64 = payloadPart
        .replace(/-/g, '+')
        .replace(/_/g, '/');

      const payload = JSON.parse(atob(base64));
      const employeeId = payload['EmployeeId'];

      if (employeeId === undefined || employeeId === null) {
        return null;
      }

      const id = Number(employeeId);

      return Number.isInteger(id) && id > 0 ? id : null;
    } catch (error) {
      console.error('Failed to read EmployeeId from token:', error);
      return null;
    }
  }

  isAdmin(): boolean {
    return this.getUserRole()?.toLowerCase() === 'admin';
  }

}
