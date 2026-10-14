
import { CommonModule } from '@angular/common';
import { Component, inject, ChangeDetectorRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import {
  AuditLog,
  AuditLogService
} from '../../../core/services/audit-log.service';

import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-leave-history',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './leave-history.html',
  styleUrl: './leave-history.css'
})
export class LeaveHistory {
  private auditLogService = inject(AuditLogService);
  private authService = inject(AuthService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  logs: AuditLog[] = [];

  page = 1;
  pageSize = 20;
  totalCount = 0;
  totalPages = 0;

  selectedAction = '';
  loading = false;
  errorMessage = '';

  constructor() {
    this.loadLogs();
  }

  loadLogs(): void {
    this.loading = true;
    this.errorMessage = '';

    this.auditLogService
      .getLogs(this.page, this.pageSize, this.selectedAction)
      .subscribe({
        next: (result) => {
          this.logs = result.items;
          this.totalCount = result.totalCount;
          this.totalPages = result.totalPages;
          this.page = result.page;
          this.loading = false;

          this.cdr.detectChanges();
        },
        error: (error) => {
          this.loading = false;
          this.errorMessage =
            error.error?.message ||
            'Unable to load leave history. Please try again.';

          this.cdr.detectChanges();
        }
      });
  }

  onFilterChange(): void {
    this.page = 1;
    this.loadLogs();
  }

  previousPage(): void {
    if (this.page > 1) {
      this.page--;
      this.loadLogs();
    }
  }

  nextPage(): void {
    if (this.page < this.totalPages) {
      this.page++;
      this.loadLogs();
    }
  }

  goBack(): void {
    this.router.navigate(['/admin-dashboard']);
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
