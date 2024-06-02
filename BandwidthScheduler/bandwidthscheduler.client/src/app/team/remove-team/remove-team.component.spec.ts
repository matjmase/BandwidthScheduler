import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RemoveTeamComponent } from './remove-team.component';

describe('RemoveTeamComponent', () => {
  let component: RemoveTeamComponent;
  let fixture: ComponentFixture<RemoveTeamComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [RemoveTeamComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(RemoveTeamComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
