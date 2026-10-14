import { TestBed } from '@angular/core/testing';
import { LeaveEntitlement } from './leave-entitlement';

describe('LeaveEntitlement', () => {
  let service: LeaveEntitlement;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(LeaveEntitlement);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
