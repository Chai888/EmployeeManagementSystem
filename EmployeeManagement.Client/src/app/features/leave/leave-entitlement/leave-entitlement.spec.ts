import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LeaveEntitlement } from './leave-entitlement';

describe('LeaveEntitlement', () => {
  let component: LeaveEntitlement;
  let fixture: ComponentFixture<LeaveEntitlement>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LeaveEntitlement],
    }).compileComponents();

    fixture = TestBed.createComponent(LeaveEntitlement);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
