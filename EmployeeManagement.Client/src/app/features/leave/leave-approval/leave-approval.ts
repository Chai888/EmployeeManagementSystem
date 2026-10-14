
import { CommonModule } from '@angular/common';
import { Component, inject, ChangeDetectorRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';

import { LeaveService } from '../../../core/services/leave.service';
import { AuthService } from '../../../core/services/auth.service';
import { LeaveRequest } from '../models/leave.model';

@Component({
  selector: 'app-leave-approval',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './leave-approval.html',
  styleUrl: './leave-approval.css'
})
export class LeaveApproval {
  private leaveService = inject(LeaveService);
  private authService = inject(AuthService);
  private router = inject(Router);

  leaveRequests: LeaveRequest[] = [];
  selectedStatus = 'Pending';

  loading = false;
  processingId: number | null = null;

  errorMessage = '';
  successMessage = '';

  constructor(
    private cdr: ChangeDetectorRef
) {
    this.loadLeaveRequests();
  }

  loadLeaveRequests(): void {
    this.loading = true;
    this.errorMessage = '';

    const status =
      this.selectedStatus === 'All'
        ? undefined
        : this.selectedStatus;

    this.leaveService.getAllLeaveRequests(status).subscribe({
      next: (requests) => {
        this.leaveRequests = requests;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (error: HttpErrorResponse) => {
        this.leaveRequests = [];
        this.loading = false;
        this.cdr.detectChanges();

        if (error.status === 401) {
          this.errorMessage =
            'Your session has expired. Please log in again.';
        } else if (error.status === 403) {
          this.errorMessage =
            'Access denied. Only Admin users can view leave requests.';
        } else {
          this.errorMessage =
            this.getErrorMessage(
              error,
              'Unable to load leave requests. Please try again.'
            );
        }
      }
    });
  }

  onStatusChange(): void {
    this.loadLeaveRequests();
  }

  approveLeave(request: LeaveRequest): void {
    this.updateLeaveStatus(request, 'Approved');
  }

  rejectLeave(request: LeaveRequest): void {
    this.updateLeaveStatus(request, 'Rejected');
  }

  private updateLeaveStatus(
    request: LeaveRequest,
    status: 'Approved' | 'Rejected'
  ): void {
    const action = status.toLowerCase();

    const confirmed = window.confirm(
      `Are you sure you want to ${action} the leave request from ${request.employeeName}?`
    );

    if (!confirmed) {
      return;
    }

    this.processingId = request.id;
    this.errorMessage = '';
    this.successMessage = '';

    this.leaveService.updateLeaveStatus(request.id, { status }).subscribe({
      next: () => {
        this.processingId = null;
        this.successMessage =
          status === 'Approved'
            ? 'Leave request approved successfully.'
            : 'Leave request rejected successfully.';
        this.errorMessage = '';

        this.loadLeaveRequests();
        this.cdr.detectChanges();
      },
      error: (error) => {
        this.processingId = null;
        this.errorMessage = this.getErrorMessage(error, `Unable to ${action} this leave request.`);
        this.successMessage = '';
        this.cdr.detectChanges();
      }
    });
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
