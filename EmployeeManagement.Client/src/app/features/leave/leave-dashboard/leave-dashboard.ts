
import { Component, OnInit, ChangeDetectorRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';

import { LeaveService } from '../../../core/services/leave.service';
import { LeaveBalance, LeaveRequest } from '../models/leave.model';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-leave-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './leave-dashboard.html',
  styleUrl: './leave-dashboard.css'
})
export class LeaveDashboard implements OnInit {
  private leaveService = inject(LeaveService);
  private authService = inject(AuthService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  currentYear = new Date().getFullYear();

  balances: LeaveBalance[] = [];
  leaveRequests: LeaveRequest[] = [];

  balanceLoading = true;
  requestsLoading = true;

  balanceMessage = '';
  errorMessage = '';
  showErrorMessage = true;

  ngOnInit(): void {
    this.loadBalance();
    this.loadLeaveRequests();
  }


  loadBalance(): void {
    this.balanceLoading = true;
    this.balanceMessage = '';

    this.leaveService.getMyBalances(this.currentYear).subscribe({
      next: balances => {
        this.balances = balances ?? [];
        this.balanceLoading = false;
        this.cdr.detectChanges();
      },
      error: error => {
        console.error('Failed to load leave balances:', error);

        this.balances = [];
        this.balanceLoading = false;

        this.balanceMessage =
          error.status === 401 || error.status === 403
            ? 'You do not have permission to view your leave balances.'
            : 'Unable to load your leave balances. Please try again.';

        this.cdr.detectChanges();
      }
    });
  }

  getUsagePercentage(balance: LeaveBalance): number {
    if (balance.entitledDays <= 0) {
      return 0;
    }

    const percentage =
      (balance.usedDays / balance.entitledDays) * 100;

    return Math.min(100, Math.max(0, Math.round(percentage)));
  }

  loadLeaveRequests(): void {
    this.requestsLoading = true;
    this.errorMessage = '';
    this.showErrorMessage = true;

    this.leaveService.getMyLeaveRequests().subscribe({
      next: requests => {
        this.leaveRequests = requests;
        this.requestsLoading = false;
        this.cdr.detectChanges();
      },
      error: error => {
        console.error('Failed to load leave requests:', error);

        this.requestsLoading = false;
        this.errorMessage = error.status === 401 || error.status === 403
          ? 'You do not have permission to view your leave requests.'
          : 'Failed to load your leave requests. Please try again.';

        this.showErrorMessage = true;
        this.cdr.detectChanges();
      }
    });
  }

  get employeeId(): number | null {
    return this.authService.getEmployeeId();
  }

  get isAdmin(): boolean {
    return this.authService.isAdmin();
  }

  get pendingCount(): number {
    return this.leaveRequests.filter(
      request => request.status === 'Pending'
    ).length;
  }

  get approvedCount(): number {
    return this.leaveRequests.filter(
      request => request.status === 'Approved'
    ).length;
  }

  get rejectedCount(): number {
    return this.leaveRequests.filter(
      request => request.status === 'Rejected'
    ).length;
  }

  getStatusClass(status: string): string {
    switch (status.toLowerCase()) {
      case 'approved':
        return 'status-approved';
      case 'rejected':
        return 'status-rejected';
      default:
        return 'status-pending';
    }
  }

  applyForLeave(): void {
    this.router.navigate(['/leave/apply']);
  }

  closeErrorMessage(): void {
    this.showErrorMessage = false;
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
