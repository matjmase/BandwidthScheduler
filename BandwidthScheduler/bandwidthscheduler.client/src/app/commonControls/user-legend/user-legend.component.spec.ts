import { ComponentFixture, TestBed } from '@angular/core/testing';

import { UserLegendComponent } from './user-legend.component';

describe('UserLegendComponent', () => {
  let component: UserLegendComponent;
  let fixture: ComponentFixture<UserLegendComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [UserLegendComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(UserLegendComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
