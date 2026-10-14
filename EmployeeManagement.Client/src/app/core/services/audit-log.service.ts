
import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface AuditLog {
  id: number;
  userId: number | null;
  username: string;
  action: string;
  entityType: string;
  entityId: string | null;
  details: string | null;
  createdAtUtc: string;
}

export interface AuditLogResult {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  items: AuditLog[];
}

@Injectable({
  providedIn: 'root'
})
export class AuditLogService {
  private http = inject(HttpClient);

  private readonly apiUrl =
    `${environment.apiUrl}/AuditLog`;

  getLogs(
    page: number,
    pageSize: number,
    action: string = ''
  ): Observable<AuditLogResult> {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize)
      .set('entityType', 'Leave');

    if (action) {
      params = params.set('action', action);
    }

    return this.http.get<AuditLogResult>(
      this.apiUrl,
      { params }
    );
  }
}
