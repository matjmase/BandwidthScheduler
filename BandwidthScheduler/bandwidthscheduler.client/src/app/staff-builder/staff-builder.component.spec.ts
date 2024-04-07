import { ComponentFixture, TestBed } from '@angular/core/testing';

import { StaffBuilderComponent } from './staff-builder.component';

describe('StaffBuilderComponent', () => {
  let component: StaffBuilderComponent;
  let fixture: ComponentFixture<StaffBuilderComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [StaffBuilderComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(StaffBuilderComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
