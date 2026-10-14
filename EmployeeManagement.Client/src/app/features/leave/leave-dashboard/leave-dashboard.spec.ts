import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LeaveDashboard } from './leave-dashboard';

describe('LeaveDashboard', () => {
  let component: LeaveDashboard;
  let fixture: ComponentFixture<LeaveDashboard>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LeaveDashboard],
    }).compileComponents();

    fixture = TestBed.createComponent(LeaveDashboard);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
