
import { Component, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-change-password',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './change-password.html',
  styleUrl: './change-password.css'
})
export class ChangePassword {
  private authService = inject(AuthService);
  private cdr = inject(ChangeDetectorRef);

  currentPassword = '';
  newPassword = '';
  confirmNewPassword = '';

  showCurrentPassword = false;
  showNewPassword = false;
  showConfirmPassword = false;

  isSubmitting = false;
  errorMessage = '';
  successMessage = '';

  changePassword(): void {
    this.errorMessage = '';
    this.successMessage = '';

    if (
      !this.currentPassword ||
      !this.newPassword ||
      !this.confirmNewPassword
    ) {
      this.errorMessage = 'Please fill in all password fields.';
      return;
    }

    if (this.newPassword.length < 8) {
      this.errorMessage =
        'New password must be at least 8 characters long.';
      return;
    }

    if (this.newPassword.length > 100) {
      this.errorMessage =
        'New password cannot exceed 100 characters.';
      return;
    }

    if (this.newPassword !== this.confirmNewPassword) {
      this.errorMessage =
        'New password and confirmation do not match.';
      return;
    }

    if (this.currentPassword === this.newPassword) {
      this.errorMessage =
        'New password must be different from your current password.';
      return;
    }

    this.isSubmitting = true;

    this.authService.changePassword(
      this.currentPassword,
      this.newPassword,
      this.confirmNewPassword
    ).subscribe({
      next: (response) => {
        this.successMessage = response.message;
        this.currentPassword = '';
        this.newPassword = '';
        this.confirmNewPassword = '';
        this.isSubmitting = false;
        this.cdr.detectChanges();
      },
      error: (error: HttpErrorResponse) => {
        this.errorMessage =
          error.error?.message ??
          'Unable to change password. Please try again.';

        this.isSubmitting = false;
        this.cdr.detectChanges();
      }
    });
  }

  get isAdmin(): boolean {
    return this.authService.isAdmin();
  }
}
