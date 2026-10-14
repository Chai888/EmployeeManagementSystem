import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { finalize } from 'rxjs';

import { LeaveService } from '../../../core/services/leave.service';
import { AuthService } from '../../../core/services/auth.service';
import { LeaveRequest } from '../../leave/models/leave.model';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './admin-dashboard.html',
  styleUrl: './admin-dashboard.css'
})
export class AdminDashboard implements OnInit {
  private readonly leaveService = inject(LeaveService);
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);
  private readonly cdr = inject(ChangeDetectorRef);

  pendingCount = 0;
  approvedCount = 0;
  rejectedCount = 0;

  loading = false;
  errorMessage = '';

  ngOnInit(): void {
    this.loadLeaveStatistics();
  }

  loadLeaveStatistics(): void {
    this.loading = true;
    this.errorMessage = '';
    this.cdr.markForCheck();

    this.leaveService.getAllLeaveRequests()
      .pipe(
        finalize(() => {
          this.loading = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: (requests: LeaveRequest[]) => {
          console.log('API returned:', requests);

          this.pendingCount = requests.filter(
            request => request.status?.toLowerCase() === 'pending'
          ).length;

          this.approvedCount = requests.filter(
            request => request.status?.toLowerCase() === 'approved'
          ).length;

          this.rejectedCount = requests.filter(
            request => request.status?.toLowerCase() === 'rejected'
          ).length;

          this.cdr.markForCheck();
        },
        error: (error) => {
          console.error('Failed to load leave statistics:', error);

          this.errorMessage =
            'Unable to load leave statistics. Please try again.';

          this.cdr.markForCheck();
        }
      });
  }

  logout(): void {
    this.authService.logout();
    void this.router.navigate(['/login']);
  }

  get totalRequests(): number {
    return this.pendingCount
      + this.approvedCount
      + this.rejectedCount;
  }
}
