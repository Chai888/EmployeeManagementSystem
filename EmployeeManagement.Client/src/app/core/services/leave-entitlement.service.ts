
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface LeaveEntitlement {
  id: number;
  employeeId: number;
  employeeName: string;
  year: number;
  leaveType: string;
  entitledDays: number;
  usedDays: number;
  remainingDays: number;
}

export interface SaveLeaveEntitlement {
  year: number;
  leaveType: string;
  entitledDays: number;
}

@Injectable({
  providedIn: 'root'
})
export class LeaveEntitlementService {
  private http = inject(HttpClient);

  private readonly apiUrl =
    'https://employee-api-888-dwemc5b9dkh4c4h4.malaysiawest-01.azurewebsites.net/api/LeaveEntitlement';

  getForEmployee(
    employeeId: number,
    year: number
  ): Observable<LeaveEntitlement[]> {
    return this.http.get<LeaveEntitlement[]>(
      `${this.apiUrl}/employees/${employeeId}`,
      { params: { year: year.toString() } }
    );
  }

  save(
    employeeId: number,
    entitlement: SaveLeaveEntitlement
  ): Observable<LeaveEntitlement> {
    return this.http.put<LeaveEntitlement>(
      `${this.apiUrl}/employees/${employeeId}`,
      entitlement
    );
  }
}
