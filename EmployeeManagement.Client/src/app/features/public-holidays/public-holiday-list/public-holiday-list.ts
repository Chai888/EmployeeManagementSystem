import {
  Component,
  OnInit,
  ChangeDetectorRef,
  inject
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import {
  PublicHolidayService,
  PublicHoliday,
  PublicHolidayRequest
} from '../../../core/services/public-holiday.service';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-public-holiday-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './public-holiday-list.html',
  styleUrl: './public-holiday-list.css'
})
export class PublicHolidayList implements OnInit {
  private publicHolidayService = inject(PublicHolidayService);
  private authService = inject(AuthService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  holidays: PublicHoliday[] = [];

  loading = true;
  saving = false;
  deletingId: number | null = null;

  showForm = false;
  editingId: number | null = null;

  holidayName = '';
  holidayDate = '';
  holidayDescription = '';

  selectedYear = new Date().getFullYear();
  readonly currentYear = new Date().getFullYear();
  readonly years: number[] = Array.from(
    { length: 7 },
    (_, index) => this.currentYear - 2 + index
  );

  successMessage = '';
  errorMessage = '';
  showSuccessMessage = true;
  showErrorMessage = true;

  ngOnInit(): void {
    this.loadHolidays();
  }

  loadHolidays(): void {
    this.loading = true;
    this.errorMessage = '';
    this.showErrorMessage = true;

    this.publicHolidayService.getAll(this.selectedYear).subscribe({
      next: (holidays) => {
        this.holidays = holidays;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Failed to load public holidays:', error);
        this.loading = false;

        this.errorMessage =
          error.status === 401 || error.status === 403
            ? 'You do not have permission to view public holidays.'
            : error.status === 0
              ? 'Cannot connect to the server.'
              : `Failed to load public holidays (HTTP ${error.status}).`;

        this.showErrorMessage = true;
        this.cdr.detectChanges();
      }
    });
  }

  onYearChange(): void {
    this.loadHolidays();
  }

  openAddForm(): void {
    this.editingId = null;
    this.holidayName = '';
    this.holidayDate = '';
    this.holidayDescription = '';
    this.successMessage = '';
    this.errorMessage = '';
    this.showForm = true;
  }

  openEditForm(holiday: PublicHoliday): void {
    this.editingId = holiday.id;
    this.holidayName = holiday.name;
    this.holidayDate = holiday.date.substring(0, 10);
    this.holidayDescription = holiday.description ?? '';
    this.successMessage = '';
    this.errorMessage = '';
    this.showForm = true;
  }

  cancelForm(): void {
    this.showForm = false;
    this.editingId = null;
    this.holidayName = '';
    this.holidayDate = '';
    this.holidayDescription = '';
    this.saving = false;
    this.errorMessage = '';
  }

  saveHoliday(): void {
    const name = this.holidayName.trim();
    const date = this.holidayDate;
    const description = this.holidayDescription.trim();

    this.errorMessage = '';
    this.showErrorMessage = true;

    if (!name) {
      this.errorMessage = 'Public holiday name is required.';
      return;
    }

    if (name.length > 100) {
      this.errorMessage =
        'Public holiday name cannot exceed 100 characters.';
      return;
    }

    if (!date) {
      this.errorMessage = 'Public holiday date is required.';
      return;
    }

    if (description.length > 250) {
      this.errorMessage =
        'Description cannot exceed 250 characters.';
      return;
    }

    if (this.saving) {
      return;
    }

    const payload: PublicHolidayRequest = {
      name,
      date,
      description: description || null
    };

    this.saving = true;

    const request = this.editingId === null
      ? this.publicHolidayService.create(payload)
      : this.publicHolidayService.update(this.editingId, payload);

    request.subscribe({
      next: () => {
        const isEditing = this.editingId !== null;

        this.saving = false;
        this.showForm = false;
        this.editingId = null;
        this.holidayName = '';
        this.holidayDate = '';
        this.holidayDescription = '';

        this.successMessage = isEditing
          ? 'Public holiday updated successfully.'
          : 'Public holiday created successfully.';

        this.showSuccessMessage = true;
        this.loadHolidays();
      },
      error: (error) => {
        console.error('Failed to save public holiday:', error);
        this.saving = false;

        this.errorMessage =
          error.error?.message ||
          (error.status === 401 || error.status === 403
            ? 'You need an Admin account to manage public holidays.'
            : error.status === 0
              ? 'Cannot connect to the server.'
              : `Failed to save public holiday (HTTP ${error.status}).`);

        this.showErrorMessage = true;
        this.cdr.detectChanges();
      }
    });
  }

  deleteHoliday(holiday: PublicHoliday): void {
    if (this.deletingId !== null || this.saving) {
      return;
    }

    const confirmed = window.confirm(
      `Are you sure you want to delete "${holiday.name}" (${holiday.date.substring(0, 10)})?`
    );

    if (!confirmed) {
      return;
    }

    this.errorMessage = '';
    this.showErrorMessage = true;
    this.deletingId = holiday.id;

    this.publicHolidayService.delete(holiday.id).subscribe({
      next: () => {
        this.deletingId = null;
        this.successMessage =
          'Public holiday deleted successfully.';
        this.showSuccessMessage = true;
        this.loadHolidays();
      },
      error: (error) => {
        console.error('Failed to delete public holiday:', error);
        this.deletingId = null;

        this.errorMessage =
          error.error?.message ||
          (error.status === 401 || error.status === 403
            ? 'You need an Admin account to delete public holidays.'
            : error.status === 0
              ? 'Cannot connect to the server.'
              : `Failed to delete public holiday (HTTP ${error.status}).`);

        this.showErrorMessage = true;
        this.cdr.detectChanges();
      }
    });
  }

  closeSuccessMessage(): void {
    this.showSuccessMessage = false;
  }

  closeErrorMessage(): void {
    this.showErrorMessage = false;
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
