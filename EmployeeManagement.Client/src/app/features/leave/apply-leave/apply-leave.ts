
import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { forkJoin } from 'rxjs';

import { LeaveService } from '../../../core/services/leave.service';
import {
  CreateLeaveRequest,
  LeaveBalance,
  LeaveRequest
} from '../models/leave.model';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-apply-leave',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './apply-leave.html',
  styleUrl: './apply-leave.css'
})
export class ApplyLeave implements OnInit {
  private leaveService = inject(LeaveService);
  private router = inject(Router);
  private authService = inject(AuthService);
  private cdr = inject(ChangeDetectorRef);

  currentYear = new Date().getFullYear();

  balances: LeaveBalance[] = [];
  balance: LeaveBalance | null = null;
  leaveRequests: LeaveRequest[] = [];

  leaveType = '';
  startDate = '';
  endDate = '';
  reason = '';

  loadingBalance = true;
  balanceError = '';
  submitting = false;
  errorMessage = '';
  successMessage = '';

  requestedDays = 0;
  calculatingRequestedDays = false;
  private calculationRequestId = 0;

  ngOnInit(): void {
    this.loadLeaveData();
  }

  loadLeaveData(): void {
    this.loadingBalance = true;
    this.balanceError = '';

    forkJoin({
      balances: this.leaveService.getMyBalances(this.currentYear),
      requests: this.leaveService.getMyLeaveRequests()
    }).subscribe({
      next: ({ balances, requests }) => {
        this.balances = balances;
        this.leaveRequests = requests;

        // Select Annual Leave when available; otherwise select
        // the first leave type configured for this employee.
        const annualLeave = this.balances.find(
          item => item.leaveType?.trim().toLowerCase() === 'annual leave'
        );

        if (
          !this.balances.some(
            item =>
              item.leaveType?.trim().toLowerCase() ===
              this.leaveType.trim().toLowerCase()
          )
        ) {
          this.leaveType = annualLeave?.leaveType
            ?? this.balances[0]?.leaveType
            ?? '';
        }

        this.updateSelectedBalance();

        this.loadingBalance = false;
        this.cdr.detectChanges();
      },
      error: error => {
        console.error('Failed to load leave data:', error);

        this.loadingBalance = false;
        this.balanceError =
          'Unable to load your leave balance and requests. Please return to the Leave Dashboard and try again.';

        this.cdr.detectChanges();
      }
    });
  }

  onLeaveTypeChange(): void {
    this.updateSelectedBalance();
    this.errorMessage = '';
    this.cdr.detectChanges();
  }

  private updateSelectedBalance(): void {
    const selectedType = this.leaveType.trim().toLowerCase();

    this.balance = selectedType
      ? this.balances.find(
        item =>
          item.leaveType?.trim().toLowerCase() === selectedType
      ) ?? null
      : null;
  }
  // Calendar-day count for display only.
  // The backend remains responsible for working-day validation.
  calculateRequestedDays(): void {
    const requestId = ++this.calculationRequestId;

    if (
      !this.startDate ||
      !this.endDate ||
      this.endDate < this.startDate
    ) {
      this.requestedDays = 0;
      this.calculatingRequestedDays = false;
      this.cdr.detectChanges();
      return;
    }

    const start = new Date(`${this.startDate}T00:00:00`);
    const end = new Date(`${this.endDate}T00:00:00`);

    if (
      start.getFullYear() !== this.currentYear ||
      end.getFullYear() !== this.currentYear
    ) {
      this.requestedDays = 0;
      this.calculatingRequestedDays = false;
      this.cdr.detectChanges();
      return;
    }

    this.calculatingRequestedDays = true;

    this.leaveService
      .getWorkingDays(this.startDate, this.endDate)
      .subscribe({
        next: days => {
          if (requestId !== this.calculationRequestId) {
            return;
          }

          this.requestedDays = days;
          this.calculatingRequestedDays = false;
          this.cdr.detectChanges();
        },
        error: error => {
          if (requestId !== this.calculationRequestId) {
            return;
          }

          console.error('Failed to calculate working days:', error);

          this.requestedDays = 0;
          this.calculatingRequestedDays = false;
          this.errorMessage =
            'Unable to calculate working days. Please try again.';
          this.cdr.detectChanges();
        }
      });
  }

  // Pending days are used for display only.
  // Do not subtract them again if the backend balance already includes them.
  get pendingDays(): number {
    if (!this.balance) {
      return 0;
    }

    return this.leaveRequests
      .filter(request =>
        request.status?.toLowerCase() === 'pending' &&
        request.leaveType?.toLowerCase() === this.leaveType.toLowerCase()
      )
      .reduce((total, request) => {
        return total + this.countDaysInYear(
          request.startDate,
          request.endDate,
          this.currentYear
        );
      }, 0);
  }

  private countDaysInYear(
    startDate: string,
    endDate: string,
    year: number
  ): number {
    const start = new Date(`${startDate.substring(0, 10)}T00:00:00`);
    const end = new Date(`${endDate.substring(0, 10)}T00:00:00`);

    const yearStart = new Date(year, 0, 1);
    const yearEnd = new Date(year, 11, 31);

    const clippedStart = start > yearStart ? start : yearStart;
    const clippedEnd = end < yearEnd ? end : yearEnd;

    if (
      Number.isNaN(clippedStart.getTime()) ||
      Number.isNaN(clippedEnd.getTime()) ||
      clippedEnd < clippedStart
    ) {
      return 0;
    }

    return Math.floor(
      (Date.UTC(
        clippedEnd.getFullYear(),
        clippedEnd.getMonth(),
        clippedEnd.getDate()
      ) -
        Date.UTC(
          clippedStart.getFullYear(),
          clippedStart.getMonth(),
          clippedStart.getDate()
        )) / 86400000
    ) + 1;
  }

  // The backend balance should already account for UsedDays and Pending requests.
  get availableDays(): number {
    return Math.max(0, this.balance?.remainingDays ?? 0);
  }

  get insufficientBalance(): boolean {
    return this.requestedDays > 0 &&
      this.requestedDays > this.availableDays;
  }

  submitLeave(): void {
    this.errorMessage = '';
    this.successMessage = '';

    if (this.loadingBalance || !this.balance) {
      this.errorMessage =
        'Leave balance is unavailable. Please try again later.';
      return;
    }

    if (!this.startDate || !this.endDate) {
      this.errorMessage =
        'Please select both the start date and end date.';
      return;
    }

    if (this.endDate < this.startDate) {
      this.errorMessage =
        'The end date cannot be earlier than the start date.';
      return;
    }

    const start = new Date(`${this.startDate}T00:00:00`);
    const end = new Date(`${this.endDate}T00:00:00`);

    if (
      start.getFullYear() !== this.currentYear ||
      end.getFullYear() !== this.currentYear
    ) {
      this.errorMessage =
        `Please select dates within ${this.currentYear}.`;
      return;
    }

    if (this.calculatingRequestedDays) {
      this.errorMessage = 'Please wait for the working-day calculation to finish.';
      return;
    }

    const request: CreateLeaveRequest = {
      leaveType: this.leaveType,
      startDate: this.startDate,
      endDate: this.endDate,
      reason: this.reason.trim()
    };

    this.submitting = true;

    this.leaveService.applyForLeave(request).subscribe({
      next: () => {
        this.submitting = false;
        this.router.navigate(['/leave'], {
          state: {
            successMessage: 'Leave request submitted successfully.'
          }
        });
      },
      error: error => {
        console.error('Failed to submit leave request:', error);

        this.submitting = false;

        const backendError = error.error;

        this.errorMessage =
          (typeof backendError === 'string' ? backendError : null) ||
          backendError?.message ||
          backendError?.title ||
          (error.status === 400
            ? 'Your leave request was rejected. Please check your available balance and selected dates.'
            : '') ||
          'Unable to submit your leave request. Please try again.';

        this.cdr.detectChanges();
      }
    });
  }

  cancel(): void {
    this.router.navigate(['/leave']);
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  get employeeId(): number | null {
    return this.authService.getEmployeeId();
  }
}
