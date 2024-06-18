import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DateRangeSelectorSubmitComponent } from './date-range-selector-submit.component';

describe('DateRangeSelectorSubmitComponent', () => {
  let component: DateRangeSelectorSubmitComponent;
  let fixture: ComponentFixture<DateRangeSelectorSubmitComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [DateRangeSelectorSubmitComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(DateRangeSelectorSubmitComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
