import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ScheduleRecallComponent } from './schedule-recall.component';

describe('ScheduleRecallComponent', () => {
  let component: ScheduleRecallComponent;
  let fixture: ComponentFixture<ScheduleRecallComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ScheduleRecallComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(ScheduleRecallComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
