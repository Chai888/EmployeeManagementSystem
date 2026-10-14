export interface LeaveBalance {
  employeeId: number;
  employeeName: string;
  year: number;
  leaveType: string;
  entitledDays: number;
  usedDays: number;
  remainingDays: number;
}

export interface LeaveRequest {
  id: number;
  employeeId: number;
  employeeName: string;
  leaveType: string;
  startDate: string;
  endDate: string;
  reason: string;
  status: string;
  approvedBy: number | null;
  approvedDate: string | null;
}

export interface CreateLeaveRequest {
  leaveType: string;
  startDate: string;
  endDate: string;
  reason: string;
}

export interface UpdateLeaveStatusRequest {
  status: 'Approved' | 'Rejected';
}
