import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AvailabilityBuilderComponent } from './availability-builder.component';

describe('AvailabilityBuilderComponent', () => {
  let component: AvailabilityBuilderComponent;
  let fixture: ComponentFixture<AvailabilityBuilderComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [AvailabilityBuilderComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(AvailabilityBuilderComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
