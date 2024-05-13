import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DateTimeRangeSelectorComponent } from './date-time-range-selector.component';

describe('DateTimeRangeSelectorComponent', () => {
  let component: DateTimeRangeSelectorComponent;
  let fixture: ComponentFixture<DateTimeRangeSelectorComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [DateTimeRangeSelectorComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(DateTimeRangeSelectorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
