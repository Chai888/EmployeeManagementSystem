
import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

import { LeaveBalance, LeaveRequest, CreateLeaveRequest, UpdateLeaveStatusRequest } from '../../features/leave/models/leave.model';

@Injectable({
  providedIn: 'root'
})
export class LeaveService {
  private http = inject(HttpClient);

  private readonly balanceUrl =
    `${environment.apiUrl}/LeaveBalance`;

  private readonly requestUrl =
    `${environment.apiUrl}/LeaveRequest`;

  getMyBalance(year: number): Observable<LeaveBalance> {
    const params = new HttpParams().set('year', year);

    return this.http.get<LeaveBalance>(
      `${this.balanceUrl}/my`,
      { params }
    );
  }

  getMyBalances(year: number): Observable<LeaveBalance[]> {
    return this.http.get<LeaveBalance[]>(
      `${this.balanceUrl}/my/all`,
      { params: { year } }
    );
  }

  getMyLeaveRequests(): Observable<LeaveRequest[]> {
    return this.http.get<LeaveRequest[]>(
      `${this.requestUrl}/my`
    );
  }

  applyForLeave(
    request: CreateLeaveRequest
  ): Observable<LeaveRequest> {
    return this.http.post<LeaveRequest>(
      this.requestUrl,
      request
    );
  }

  getAllLeaveRequests(
    status?: string
  ): Observable<LeaveRequest[]> {
    let params = new HttpParams();

    if (status) {
      params = params.set('status', status);
    }

    return this.http.get<LeaveRequest[]>(
      this.requestUrl,
      { params }
    );
  }

  updateLeaveStatus(
    id: number,
    request: UpdateLeaveStatusRequest
  ): Observable<LeaveRequest> {
    return this.http.put<LeaveRequest>(
      `${this.requestUrl}/${id}/status`,
      request
    );
  }

  cancelLeaveRequest(id: number): Observable<LeaveRequest> {
    return this.http.put<LeaveRequest>(
      `${this.requestUrl}/${id}/cancel`,
      {}
    );
  }

  getWorkingDays(
    startDate: string,
    endDate: string
  ): Observable<number> {
    const params = new HttpParams()
      .set('startDate', startDate)
      .set('endDate', endDate);

    return this.http.get<number>(
      `${this.requestUrl}/working-days`,
      { params }
    );
  }
}
