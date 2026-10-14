
import { Component, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.html',
  styleUrl: './login.css'
})
export class Login {
  private authService = inject(AuthService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  username = '';
  password = '';
  errorMessage = '';
  loading = false;
  showPassword = false;
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
  
  onLogin(): void {
    this.errorMessage = '';
	this.showErrorMessage = true;
	 
    if (!this.username.trim() || !this.password) {
      this.errorMessage = 'Please enter username and password.';
      return;
    }
	
	// Prevent multiple simultaneous requests
	if (this.loading) {
	  return;
	}
  

    this.loading = true;

    if (this.authService.isDemoMode()) {
      const success = this.authService.loginDemo(
        this.username,
        this.password
      );

      this.loading = false;

      if (!success) {
        this.errorMessage =
          'Invalid demo credentials. Please check the demo accounts.';
        this.showErrorMessage = true;
        return;
      }

      const role = this.authService.getUserRole()?.toLowerCase();

      if (role === 'admin') {
        this.router.navigate(['/admin-dashboard']);
      } else {
        this.router.navigate(['/leave']);
      }

      return;
    }

    this.authService.login(this.username.trim(), this.password)
      .subscribe({
        next: () => {
          this.loading = false;

          const role = this.authService.getUserRole()?.toLowerCase();

          if (role === 'admin') {
            this.router.navigate(['/admin-dashboard']);
          } else if (role === 'employee') {
            this.router.navigate(['/leave']);
          } else {
            this.authService.logout();
            this.errorMessage =
              'Unable to determine your user role. Please contact the administrator.';
            this.showErrorMessage = true;
            this.cdr.detectChanges();
          }
        },
        error: (error) => {
          console.error('Login failed:', error);
          this.loading = false;
          this.showErrorMessage = true;

          if (error.status === 401 || error.status === 400) {
            this.errorMessage = 'Invalid username or password.';
          } else if (error.status === 0) {
            this.errorMessage =
              'Cannot connect to the server. Please try again.';
          } else {
            this.errorMessage =
              error.error?.message ||
              `Login failed (HTTP ${error.status || 'unknown'}).`;
          }

          this.cdr.detectChanges();
        }
      });
  }
}
