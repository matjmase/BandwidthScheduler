import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AvailabilityAndCommitmentsComponent } from './availability-and-commitments.component';

describe('AvailabilityAndCommitmentsComponent', () => {
  let component: AvailabilityAndCommitmentsComponent;
  let fixture: ComponentFixture<AvailabilityAndCommitmentsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [AvailabilityAndCommitmentsComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(AvailabilityAndCommitmentsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
