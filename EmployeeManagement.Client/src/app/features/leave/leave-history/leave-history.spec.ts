import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LeaveHistory } from './leave-history';

describe('LeaveHistory', () => {
  let component: LeaveHistory;
  let fixture: ComponentFixture<LeaveHistory>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LeaveHistory],
    }).compileComponents();

    fixture = TestBed.createComponent(LeaveHistory);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
