import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LeaveApproval } from './leave-approval';

describe('LeaveApproval', () => {
  let component: LeaveApproval;
  let fixture: ComponentFixture<LeaveApproval>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LeaveApproval],
    }).compileComponents();

    fixture = TestBed.createComponent(LeaveApproval);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
