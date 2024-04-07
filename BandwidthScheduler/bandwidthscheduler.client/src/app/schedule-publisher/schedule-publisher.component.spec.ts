import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SchedulePublisherComponent } from './schedule-publisher.component';

describe('SchedulePublisherComponent', () => {
  let component: SchedulePublisherComponent;
  let fixture: ComponentFixture<SchedulePublisherComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [SchedulePublisherComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(SchedulePublisherComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
