import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TimePickerSnapComponent } from './time-picker-snap.component';

describe('TimePickerSnapComponent', () => {
  let component: TimePickerSnapComponent;
  let fixture: ComponentFixture<TimePickerSnapComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [TimePickerSnapComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(TimePickerSnapComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
