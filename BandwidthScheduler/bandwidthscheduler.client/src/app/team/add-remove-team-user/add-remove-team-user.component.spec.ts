import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AddRemoveTeamUserComponent } from './add-remove-team-user.component';

describe('AddRemoveTeamUserComponent', () => {
  let component: AddRemoveTeamUserComponent;
  let fixture: ComponentFixture<AddRemoveTeamUserComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [AddRemoveTeamUserComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(AddRemoveTeamUserComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
