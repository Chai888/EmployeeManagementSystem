import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface PublicHoliday {
  id: number;
  name: string;
  date: string;
  description: string | null;
}

export interface PublicHolidayRequest {
  name: string;
  date: string;
  description: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class PublicHolidayService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/PublicHoliday`;

  getAll(year?: number): Observable<PublicHoliday[]> {
    const url = year
      ? `${this.apiUrl}?year=${year}`
      : this.apiUrl;

    return this.http.get<PublicHoliday[]>(url);
  }

  getById(id: number): Observable<PublicHoliday> {
    return this.http.get<PublicHoliday>(`${this.apiUrl}/${id}`);
  }

  create(request: PublicHolidayRequest): Observable<PublicHoliday> {
    return this.http.post<PublicHoliday>(this.apiUrl, request);
  }

  update(
    id: number,
    request: PublicHolidayRequest
  ): Observable<PublicHoliday> {
    return this.http.put<PublicHoliday>(
      `${this.apiUrl}/${id}`,
      request
    );
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
